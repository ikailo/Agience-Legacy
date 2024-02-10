using QuikGraph;

namespace Agience.Client
{
    public class InformationEdge : Edge<InformationVertex>
    {
        internal InformationEdge(InformationVertex source, InformationVertex target) : base(source, target)
        {
        }
    }
}
