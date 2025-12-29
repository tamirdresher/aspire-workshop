@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource cosmosdb 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
  name: take('cosmosdb-${uniqueString(resourceGroup().id)}', 44)
  location: location
  properties: {
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    databaseAccountOfferType: 'Standard'
    disableLocalAuth: true
  }
  kind: 'GlobalDocumentDB'
  tags: {
    'aspire-resource-name': 'cosmosdb'
  }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
  name: 'cosmos'
  location: location
  properties: {
    resource: {
      id: 'cosmos'
    }
  }
  parent: cosmosdb
}

resource books 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: 'books'
  location: location
  properties: {
    resource: {
      id: 'books'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
  parent: cosmos
}

resource carts 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: 'carts'
  location: location
  properties: {
    resource: {
      id: 'carts'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
  parent: cosmos
}

resource orders 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: 'orders'
  location: location
  properties: {
    resource: {
      id: 'orders'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
  parent: cosmos
}

output connectionString string = cosmosdb.properties.documentEndpoint

output name string = cosmosdb.name