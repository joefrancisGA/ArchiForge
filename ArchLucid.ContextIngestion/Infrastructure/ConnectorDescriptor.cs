using ArchLucid.ContextIngestion.Interfaces;

namespace ArchLucid.ContextIngestion.Infrastructure;

public sealed class ConnectorDescriptor : IConnectorDescriptor
{
    public ConnectorDescriptor(int pipelineOrder, IContextConnector connector)
    {
        ArgumentNullException.ThrowIfNull(connector);
        PipelineOrder = pipelineOrder;
        Connector = connector;
    }

    public int PipelineOrder
    {
        get;
    }

    public IContextConnector Connector
    {
        get;
    }
}
