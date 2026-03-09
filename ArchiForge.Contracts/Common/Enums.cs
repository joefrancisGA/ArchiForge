namespace ArchiForge.Contracts.Common;

public enum CloudProvider
{
    Azure = 1
}

public enum ArchitectureRunStatus
{
    Created = 1,
    TasksGenerated = 2,
    WaitingForResults = 3,
    ReadyForCommit = 4,
    Committed = 5,
    Failed = 6
}

public enum AgentTaskStatus
{
    Created = 1,
    InProgress = 2,
    Completed = 3,
    Rejected = 4,
    Failed = 5
}

public enum AgentType
{
    Topology = 1,
    Cost = 2,
    Compliance = 3,
    Critic = 4
}

public enum ServiceType
{
    Api = 1,
    Worker = 2,
    Ui = 3,
    Integration = 4,
    DataService = 5,
    SearchService = 6,
    AiService = 7
}

public enum RuntimePlatform
{
    AppService = 1,
    Functions = 2,
    Aks = 3,
    Vm = 4,
    ContainerApps = 5,
    SqlServer = 6,
    AzureAiSearch = 7,
    AzureOpenAi = 8,
    Redis = 9,
    BlobStorage = 10,
    KeyVault = 11
}

public enum DatastoreType
{
    Sql = 1,
    NoSql = 2,
    Object = 3,
    Cache = 4,
    Search = 5
}

public enum RelationshipType
{
    Calls = 1,
    ReadsFrom = 2,
    WritesTo = 3,
    PublishesTo = 4,
    SubscribesTo = 5,
    AuthenticatesWith = 6
}