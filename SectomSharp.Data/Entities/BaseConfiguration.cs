using JetBrains.Annotations;

namespace SectomSharp.Data.Entities;

public abstract class BaseConfiguration
{
    public bool IsDisabled { get; [UsedImplicitly(Reason = Constants.ValueGeneratedOnAdd)] set; }
}
