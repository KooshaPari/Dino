using System;
using System.Collections.Generic;
using System.Linq;
using DINOForge.SDK.Registry;

namespace DINOForge.Runtime.Generation
{
    internal enum GenerationConsumerDisposition
    {
        LookupOnDemand,
        MaterializedReconciliable,
        NewOnly,
        RestartRequired
    }

    internal enum GenerationConsumerStatus
    {
        Pending,
        Ack,
        Nack,
        RestartRequired,
        Partial
    }

    internal sealed class RuntimeGeneration
    {
        public string Id { get; }
        public RegistryManager Registries { get; }

        public RuntimeGeneration(string id, RegistryManager registries)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Registries = registries ?? throw new ArgumentNullException(nameof(registries));
        }
    }

    internal sealed class GenerationConsumerResult
    {
        public string ConsumerId { get; }
        public string DesiredGeneration { get; }
        public string? ObservedGeneration { get; }
        public string? MaterializedGeneration { get; }
        public GenerationConsumerDisposition Disposition { get; }
        public GenerationConsumerStatus Status { get; }
        public string? Detail { get; }

        public GenerationConsumerResult(
            string consumerId,
            string desiredGeneration,
            GenerationConsumerDisposition disposition,
            GenerationConsumerStatus status,
            string? observedGeneration = null,
            string? materializedGeneration = null,
            string? detail = null)
        {
            ConsumerId = consumerId;
            DesiredGeneration = desiredGeneration;
            Disposition = disposition;
            Status = status;
            ObservedGeneration = observedGeneration;
            MaterializedGeneration = materializedGeneration;
            Detail = detail;
        }

        public bool Qualifies =>
            Status == GenerationConsumerStatus.Ack &&
            string.Equals(ObservedGeneration, DesiredGeneration, StringComparison.Ordinal) &&
            (Disposition != GenerationConsumerDisposition.MaterializedReconciliable ||
             string.Equals(MaterializedGeneration, DesiredGeneration, StringComparison.Ordinal));
    }

    internal sealed class GenerationActivationReceipt
    {
        public string DesiredGeneration { get; }
        public string? PriorActiveGeneration { get; }
        public string? ActiveGeneration { get; }
        public IReadOnlyDictionary<string, GenerationConsumerResult> Consumers { get; }
        public bool Committed { get; }

        public GenerationActivationReceipt(
            string desiredGeneration,
            string? priorActiveGeneration,
            string? activeGeneration,
            IReadOnlyDictionary<string, GenerationConsumerResult> consumers,
            bool committed)
        {
            DesiredGeneration = desiredGeneration;
            PriorActiveGeneration = priorActiveGeneration;
            ActiveGeneration = activeGeneration;
            Consumers = consumers;
            Committed = committed;
        }
    }

    /// <summary>
    /// Integration candidate only. Owns generation publication and consumer
    /// acknowledgement; it does not yet replace ModPlatform loading.
    /// </summary>
    internal sealed class GenerationStore
    {
        private readonly HashSet<string> _requiredConsumers;
        private readonly Dictionary<string, GenerationConsumerResult> _results = new(StringComparer.Ordinal);

        public RuntimeGeneration? Active { get; private set; }
        public RuntimeGeneration? Proposed { get; private set; }

        public GenerationStore(IEnumerable<string> requiredConsumers)
        {
            _requiredConsumers = new HashSet<string>(requiredConsumers ?? Array.Empty<string>(), StringComparer.Ordinal);
        }

        public void SeedActive(RuntimeGeneration generation)
        {
            if (Proposed != null)
                throw new InvalidOperationException("Cannot reseed while a generation proposal is active.");
            Active = generation;
        }

        public void BeginProposal(RuntimeGeneration candidate)
        {
            Proposed = candidate ?? throw new ArgumentNullException(nameof(candidate));
            _results.Clear();
        }

        public bool Record(GenerationConsumerResult result)
        {
            if (Proposed == null ||
                !string.Equals(result.DesiredGeneration, Proposed.Id, StringComparison.Ordinal) ||
                !_requiredConsumers.Contains(result.ConsumerId))
                return false;
            _results[result.ConsumerId] = result;
            return true;
        }

        public GenerationActivationReceipt Evaluate()
        {
            if (Proposed == null)
                throw new InvalidOperationException("No generation proposal is active.");

            string? prior = Active?.Id;
            bool allQualified =
                _requiredConsumers.Count == _results.Count &&
                _requiredConsumers.All(id => _results.TryGetValue(id, out var result) && result.Qualifies);

            if (allQualified)
            {
                Active = Proposed;
                Proposed = null;
            }

            return new GenerationActivationReceipt(
                desiredGeneration: allQualified ? Active!.Id : Proposed.Id,
                priorActiveGeneration: prior,
                activeGeneration: Active?.Id,
                consumers: new Dictionary<string, GenerationConsumerResult>(_results),
                committed: allQualified);
        }

        public GenerationActivationReceipt Reject(string detail)
        {
            if (Proposed == null)
                throw new InvalidOperationException("No generation proposal is active.");
            string desired = Proposed.Id;
            string? prior = Active?.Id;
            Proposed = null;
            return new GenerationActivationReceipt(
                desired, prior, Active?.Id,
                new Dictionary<string, GenerationConsumerResult>(_results),
                committed: false);
        }
    }
}
