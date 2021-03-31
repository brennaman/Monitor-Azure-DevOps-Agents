To run this code update these static varibles with real values in the Program class:

```
// Update organizationName to your ADO organization name
static string organizationName = "MyOrgName";

// Update adoPAT to your the Personal Access Token that you'll use to access the ADO Rest API
static string adoPAT = "lakjdsflkajslkdjflkajsdljflkaj";

// Update customerId to your Log Analytics workspace ID
static string customerId = "00000000-0000-0000-0000-000000000000";

// For Log Analytics sharedKey, use either the primary or the secondary Connected Sources client authentication key   
static string sharedKey = "lajdlfkjaljsdlfjlkadsjljflasjl";

```

This code performs a scan on 1 minute intervals and does so for 12 intervals before the programs exits.  To run just execute the following command: `dotnet run`