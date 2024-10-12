param containerRegistryName string
param managedEnvironmentName string
param discordContainerAppName string
param dbContainerAppName string
param discordImage string
param dbImage string
param dbMountPath string
param dbName string
param dbUser string
@secure()
param dbPassword string
@secure()
param discordToken string
param discordShards string
param discordStatus string
param discordMetricUri string
param discordRateLimitingRenewalJobExecutionInMilliSeconds string
param discordRateLimitingMaxUsages string
param discordRateLimitingUsageFallOffInMilliSeconds string
param discordFortuneUri string

var location = resourceGroup().location
var dbVolumeName = 'v-db'
var registryPasswordRef = 'password'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: '${containerRegistryName}${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
    policies: {
      quarantinePolicy: {
        status: 'disabled'
      }
      trustPolicy: {
        type: 'Notary'
        status: 'disabled'
      }
      retentionPolicy: {
        days: 7
        status: 'disabled'
      }
      exportPolicy: {
        status: 'enabled'
      }
    }
    encryption: {
      status: 'disabled'
    }
    dataEndpointEnabled: false
    publicNetworkAccess: 'Enabled'
    networkRuleBypassOptions: 'AzureServices'
    zoneRedundancy: 'Disabled'
  }
}

resource managedEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${managedEnvironmentName}-${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    zoneRedundant: false
    workloadProfiles: [
      {
        workloadProfileType: 'Consumption'
        name: 'Consumption'
      }
    ]
    peerAuthentication: {
      mtls: {
        enabled: true
      }
    }
    peerTrafficConfiguration: {
      encryption: {
        enabled: false
      }
    }
  }
}

resource discordContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${discordContainerAppName}-${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    managedEnvironmentId: managedEnvironment.id
    environmentId: managedEnvironment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.listCredentials().username
          passwordSecretRef: registryPasswordRef
        }
      ]
      secrets: [
        {
          name: registryPasswordRef
          value: containerRegistry.listCredentials().passwords[0].value
        }
      ]
      maxInactiveRevisions: 1
    }
    template: {
      containers: [
        {
          image: '${containerRegistry.properties.loginServer}/${discordImage}'
          name: '${discordContainerAppName}-${uniqueString(resourceGroup().id)}'
          env: [
            {
              name: 'DiscordSettings__Token'
              value: discordToken
            }
            {
              name: 'DiscordSettings__Shards'
              value: discordShards
            }
            {
              name: 'DiscordSettings__Status'
              value: discordStatus
            }
            {
              name: 'DiscordSettings__MetricsUri'
              value: discordMetricUri
            }
            {
              name: 'DiscordSettings__RateLimitingRenewalJobExecutionInMilliSeconds'
              value: discordRateLimitingRenewalJobExecutionInMilliSeconds
            }
            {
              name: 'DiscordSettings__RateLimitingMaxUsages'
              value: discordRateLimitingMaxUsages
            }
            {
              name: 'DiscordSettings__RateLimitingUsageFallOffInMilliSeconds'
              value: discordRateLimitingUsageFallOffInMilliSeconds
            }
            {
              name: 'DiscordSettings__FortuneUri'
              value: discordFortuneUri
            }
            {
              name: 'ConnectionStrings__DataContext'
              value: 'Host=${dbContainerAppNameFull}.internal;Database=${dbName};Username=${dbUser};Password=${dbPassword}'
            }
          ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          probes: []
          volumeMounts: []
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      volumes: []
    }
  }
}

var dbContainerAppNameFull = '${dbContainerAppName}-${uniqueString(resourceGroup().id)}'
resource dbContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: dbContainerAppNameFull
  location: location
  properties: {
    managedEnvironmentId: managedEnvironment.id
    environmentId: managedEnvironment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 5432
        exposedPort: 5432
        transport: 'tcp'
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
        allowInsecure: false
        stickySessions: {
          affinity: 'none'
        }
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.listCredentials().username
          passwordSecretRef: registryPasswordRef
        }
      ]
      secrets: [
        {
          name: registryPasswordRef
          value: containerRegistry.listCredentials().passwords[0].value
        }
      ]
      maxInactiveRevisions: 1
    }
    template: {
      containers: [
        {
          image: '${containerRegistry.properties.loginServer}/${dbImage}'
          name: '${dbContainerAppName}-${uniqueString(resourceGroup().id)}'
          env: [
            {
              name: 'POSTGRES_DB'
              value: dbName
            }
            {
              name: 'POSTGRES_USER'
              value: dbUser
            }
            {
              name: 'POSTGRES_PASSWORD'
              value:  dbPassword
            }
          ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          probes: []
          volumeMounts: [
            {
              volumeName: dbVolumeName
              mountPath: dbMountPath
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      volumes: [
        {
          name: dbVolumeName
          storageType: 'EmptyDir'
        }
      ]
    }
  }
}
