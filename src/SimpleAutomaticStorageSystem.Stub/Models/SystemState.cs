using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleAutomaticStorageSystem.Stub.Models;

public class SystemState
{
    private readonly object _lock = new();
    public bool IsPicking { get; set; } = false;

    public bool TryStartPicking() 
    {
        lock (_lock)
        {
            if (IsPicking)
            {
                return false;
            }

            IsPicking = true;
            return true;
        }
    }
    public void EndPicking()
    {
        lock (_lock)
        {
            IsPicking = false;
        }
    }
    public bool IsStoring { get; set; } = false;

    public bool TryStartStoring()
    {
        lock (_lock)
        {
            if (IsStoring)
            {
                return false;
            }

            IsStoring = true;
            return true;
        }
    }
    public void EndStoring()
    {
        lock (_lock)
        {
            IsStoring = false;
        }
    }
    public RackState State { get; set; } = RackState.Offline;
}
public enum RackState
{
    Offline,
    Online,
    Emergency,
    Fatal
}
