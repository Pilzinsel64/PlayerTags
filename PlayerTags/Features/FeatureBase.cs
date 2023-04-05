using Pilz.Dalamud.ActivityContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTags.Features
{
    public abstract class FeatureBase : IDisposable
    {
        public ActivityContextManager ActivityContextManager { get; init; }

        public FeatureBase()
        {
            ActivityContextManager = new();
        }

        public virtual void Dispose()
        {
            ActivityContextManager.Dispose();
        }
    }
}
