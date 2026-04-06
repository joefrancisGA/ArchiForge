using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Interfaces;

public interface IFindingPayloadValidator
{
    void Validate(Finding finding);
}

