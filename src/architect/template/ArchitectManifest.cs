using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace architect.template
{
    internal class ArchitectManifest
    {
        public Dictionary<string, TemplateVariable> TemplateVariables { get; set; } = new Dictionary<string, TemplateVariable>();
    }
}
