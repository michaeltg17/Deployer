using Api.Builders;
using Api.Models;

namespace Core.Testing.Builders
{
    public class DeployRequestBuilder : BuilderWithValues<DeployRequestBuilder, DeployRequest>
    {
        protected override DeployRequest Item { get; set; }

        public DeployRequestBuilder()
        {
            Item = new DeployRequest
            {
                Project = "test-project",
                Environment = "dev",
                Tag = "v1"
            };
        }
    }
}
