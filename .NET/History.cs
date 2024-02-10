using System.Collections.Concurrent;
using QuikGraph;

namespace Agience.Client
{
    public class History : BidirectionalGraph<InformationVertex, InformationEdge>
    {
        // Define a ConcurrentDictionary to store locks for each vertex
        private readonly ConcurrentDictionary<string, object> _vertexLocks = new ConcurrentDictionary<string, object>();
        //private readonly ConcurrentDictionary<string, InformationVertex> _verticesById = new ConcurrentDictionary<string, InformationVertex>();

        // Define a ReaderWriterLockSlim for write operations
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        internal void Add(Information information)
        {
            InformationVertex? currentVertex;

            // Get or create the lock for the current vertex
            var currentVertexLock = _vertexLocks.GetOrAdd(information.Id, _ => new object());

            lock (currentVertexLock)
            {
                _lock.EnterWriteLock();
                try
                {
                    // TODO: Use indexed dictionary for performance
                    currentVertex = Vertices.FirstOrDefault(v => v.Id == information.Id);

                    if (currentVertex == null)
                    {
                        currentVertex = new InformationVertex()
                        {
                            Id = information.Id,
                            Input = information.Input,
                            InputTimestamp = DateTime.TryParse(information.InputTimestamp!, out var inputTimestamp) ? inputTimestamp : null,
                            Output = information.Output,
                            OutputTimestamp = DateTime.TryParse(information.OutputTimestamp!, out var outputTimestamp) ? outputTimestamp : null,
                            Transformation = information.Transformation,
                            TemplateId = information.TemplateId
                        };
                        AddVertex(currentVertex);
                    }
                    else // TODO: Make sure we're not overwriting imporant info. Assume for now it's only been added as a parent.
                    {
                        currentVertex.Input = information.Input;
                        currentVertex.InputTimestamp = DateTime.TryParse(information.InputTimestamp!, out var inputTimestamp) ? inputTimestamp : null;
                        currentVertex.Output = information.Output;
                        currentVertex.OutputTimestamp = DateTime.TryParse(information.OutputTimestamp!, out var outputTimestamp) ? outputTimestamp : null;
                        currentVertex.Transformation = information.Transformation;
                        currentVertex.TemplateId = information.TemplateId;
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            if (string.IsNullOrEmpty(information.ParentInformationId)) { return; }

            // Get or create the lock for the parent vertex
            var parentVertexLock = _vertexLocks.GetOrAdd(information.ParentInformationId, _ => new object());

            lock (parentVertexLock)
            {
                _lock.EnterWriteLock();
                try
                {
                    // TODO: Use indexed dictionary for performance
                    var parentVertex = Vertices.FirstOrDefault(v => v.Id == information.ParentInformationId);

                    if (parentVertex == null)
                    {
                        parentVertex = new InformationVertex()
                        {
                            Id = information.ParentInformationId
                        };
                        AddVertex(parentVertex);
                    }

                    AddEdge(new InformationEdge(parentVertex, currentVertex));
                    
                    var foo = this;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }
    }
}
