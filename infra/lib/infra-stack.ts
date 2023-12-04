import * as cdk from 'aws-cdk-lib';
import * as ecs from 'aws-cdk-lib/aws-ecs';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import { Construct } from 'constructs';
import { ARecord, HostedZone, RecordTarget } from 'aws-cdk-lib/aws-route53';
import { ManagedPolicy, Role, ServicePrincipal } from 'aws-cdk-lib/aws-iam';
import { AutoScalingGroup, CfnAutoScalingGroup } from 'aws-cdk-lib/aws-autoscaling';
import { LogGroup, RetentionDays } from 'aws-cdk-lib/aws-logs';
import { Bucket } from 'aws-cdk-lib/aws-s3';

export class InfraStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const apiSubDomain = `weather`;
    const apexDomain = 'cloudchaotic.com';
    const apiDomainName = `${apiSubDomain}.${apexDomain}`;

    const vpc = ec2.Vpc.fromLookup(this, 'DefaultVPC', {
      isDefault: true
    });

    const securityGroup = new ec2.SecurityGroup(this, 'SecurityGroup', {
      vpc,
      allowAllOutbound: true
    });
    securityGroup.addIngressRule(
      ec2.Peer.anyIpv4(),
      ec2.Port.tcp(80),
      'Allow HTTP traffic from anywhere'
    );
    securityGroup.addIngressRule(
      ec2.Peer.anyIpv4(),
      ec2.Port.tcp(443),
      'Allow HTTPS traffic from anywhere'
    );

    const subnet = vpc.publicSubnets.sort(this.compareIds)[0];

    const elasticIp = new ec2.CfnEIP(this, 'ElasticIp');
    elasticIp.applyRemovalPolicy(cdk.RemovalPolicy.DESTROY);
    const networkInterface = new ec2.CfnNetworkInterface(this, 'NetworkInterface', {
      subnetId: subnet.subnetId,
      groupSet: [ securityGroup.securityGroupId ]
    });
    const eipAssociation = new ec2.CfnEIPAssociation(this, 'EIPAssociation', {
      allocationId: elasticIp.attrAllocationId,
      networkInterfaceId: networkInterface.attrId
    });
    eipAssociation.applyRemovalPolicy(cdk.RemovalPolicy.DESTROY);

    const instanceRole = new Role(this, 'InstanceRole', {
      assumedBy: new ServicePrincipal('ec2.amazonaws.com')
    });
    instanceRole.addManagedPolicy(ManagedPolicy.fromAwsManagedPolicyName('service-role/AmazonEC2ContainerServiceforEC2Role'));

    const cluster = new ecs.Cluster(this, 'Cluster', {
      vpc
    });

    const userData = ec2.UserData.forLinux();
    userData.addCommands(`echo "ECS_CLUSTER=${cluster.clusterName}" >> /etc/ecs/ecs.config`);
    const launchTemplate = new ec2.LaunchTemplate(this, 'LaunchTemplate', {
      instanceType: ec2.InstanceType.of(ec2.InstanceClass.T3, ec2.InstanceSize.NANO),
      machineImage: ecs.EcsOptimizedImage.amazonLinux2023(),
      role: instanceRole,
      userData
    });
    // L2 construct does not yet support specifying network interfaces: https://github.com/aws/aws-cdk/issues/14494
    // use cdk escape hatch in order to set network interface
    const cfnLaunchTemplate = launchTemplate.node.defaultChild as ec2.CfnLaunchTemplate;
    cfnLaunchTemplate.launchTemplateData = {
      ...cfnLaunchTemplate.launchTemplateData,
      networkInterfaces: [{
        deleteOnTermination: false,
        deviceIndex: 0,
        networkInterfaceId: networkInterface.attrId
      }]
    };

    const autoScalingGroup = new AutoScalingGroup(this, 'AutoScalingGroup', {
      vpc,
      launchTemplate,
      minCapacity: 1,
      maxCapacity: 1
    });
    const cfnAutoScalingGroup = autoScalingGroup.node.defaultChild as CfnAutoScalingGroup;
    cfnAutoScalingGroup.availabilityZones = [ subnet.availabilityZone ];
    // cannot specify subnet ID if setting existing network interface ID.
    cfnAutoScalingGroup.vpcZoneIdentifier = undefined;

    const capacityProvider = new ecs.AsgCapacityProvider(this, 'AsgCapacityProvider', {
      autoScalingGroup
    });
    cluster.addAsgCapacityProvider(capacityProvider);

    const s3Bucket = new Bucket(this, 'Bucket', {
      removalPolicy: cdk.RemovalPolicy.DESTROY
    });

    const logGroup = new LogGroup(this, 'LogGroup', {
      logGroupName: `weather-service`,
      retention: RetentionDays.ONE_MONTH,
      removalPolicy: cdk.RemovalPolicy.DESTROY
    });
    const taskDefinition = new ecs.Ec2TaskDefinition(this, 'TaskDefinition');
    taskDefinition.addContainer('WeatherAppContainer', {
      image: ecs.ContainerImage.fromAsset('../WeatherApp'),
      portMappings: [ { containerPort: 8080, hostPort: 80 }, { containerPort: 8081, hostPort: 443 } ],
      memoryReservationMiB: 128,
      environment: {
        'BUCKET_REGION': s3Bucket.env.region,
        'BUCKET_NAME': s3Bucket.bucketName,
        'DOMAIN_NAME': apiDomainName
      },
      logging: ecs.LogDrivers.awsLogs({
        streamPrefix: `weather-service`,
        logGroup
      })
    });
    s3Bucket.grantReadWrite(taskDefinition.taskRole);

    const service = new ecs.Ec2Service(this, 'EC2Service', {
      cluster,
      taskDefinition,
      desiredCount: 1,
      maxHealthyPercent: 100,
      minHealthyPercent: 0,
      circuitBreaker: { rollback: true }
    });

    const hostedZone = HostedZone.fromLookup(this, 'HostedZone', {
      domainName: apexDomain
    });

    const aRecord = new ARecord(this, 'ARecord', {
      target: RecordTarget.fromIpAddresses(elasticIp.attrPublicIp),
      zone: hostedZone,
      recordName: apiSubDomain,
      ttl: cdk.Duration.minutes(0)
    });
    aRecord.applyRemovalPolicy(cdk.RemovalPolicy.DESTROY);
  }

  private compareIds(a: ec2.ISubnet, b: ec2.ISubnet): number {
    if ( a.subnetId < b.subnetId ){
      return -1;
    }
    if ( a.subnetId > b.subnetId ){
      return 1;
    }
    return 0;
  }
}
