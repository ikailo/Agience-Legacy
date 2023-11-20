//using QuikGraph;

using System.Collections.Concurrent;

namespace Technologai
{
    // TODO: Rework all of this.

    public class Context
    {
        private readonly ConcurrentDictionary<string, Information> _library = new();
        private readonly ConcurrentDictionary<string, Template> _templates = new();
        private readonly ConcurrentDictionary<string, List<string>> _forwardContext = new();
        private readonly ConcurrentDictionary<string, List<string>> _reverseContext = new();
        private readonly ConcurrentDictionary<string, string> _lineage = new();

        public Context(Identity identity) { }

        public Information? GetPublisher(string forwardId)
        {
            return _lineage.ContainsKey(forwardId) ? _library[_lineage[forwardId]] : null;
        }

        public void Spawn(string forwardId, string reverseId)
        {
            AddForward(forwardId, reverseId);
            AddReverse(forwardId, reverseId);
            _lineage[forwardId] = reverseId;
        }

        public void Add(Information information)
        {
            _library[information.Id] = information;
        }
        
        public void AddReverse(string forwardId, string reverseId)
        {
            if (!_reverseContext.ContainsKey(forwardId))
            {
                _reverseContext[forwardId] = new List<string>();
            }
            
            _reverseContext[forwardId].Add(reverseId);            
        }

        public void AddForward(string forwardId, string reverseId)
        {
            if (!_forwardContext.ContainsKey(reverseId))
            {
                _forwardContext[reverseId] = new List<string>();
            }
            
            _forwardContext[reverseId].Add(forwardId);            
        }
        
        /*
        public List<Information> ToList(List<string> contextIds)
        {
            List<Information> result = new List<Information>();
            foreach (string contextId in contextIds)
            {
                result.Add(_library[contextId]);
            }
            return result;
        }

        public List<Information> GetForward(string reverseId)
        {
            return _forwardContext.ContainsKey(reverseId) ? ToList(_forwardContext[reverseId]) : new();
        }

        public List<Information> GetReverse(string forwardId)
        {
            return _reverseContext.ContainsKey(forwardId) ? ToList(_reverseContext[forwardId]) : new();
        }

        public string Summarize(string contextId)
        {
            var currentInfo = _library[contextId];
            string summary = $"{currentInfo.Input} {currentInfo.TemplateId} {currentInfo.Output}\n";

            foreach (Information information in GetReverse(contextId))
            {
                summary += $"{information.Input} {information.TemplateId} {information.Output}\n"; // TODO: Template Description
            }
            foreach (Information information in GetForward(contextId))
            {
                summary += $"{information.Input} {information.TemplateId} {information.Output} \n"; // TODO: Template Description
            }
            return summary;
        }*/
    }
}

