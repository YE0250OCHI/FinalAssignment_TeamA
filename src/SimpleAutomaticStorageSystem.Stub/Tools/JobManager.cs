using SimpleAutomaticStorageSystem.Stub.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace SimpleAutomaticStorageSystem.Stub.Tools;

internal class JobManager
{
    private readonly ConcurrentDictionary<string, byte> _receivedJobs = new();

    private readonly ConcurrentQueue<JobBody> _pickingJobs = new();
    private readonly ConcurrentQueue<JobBody> _storingJobs = new();

    public bool TryAddJob(JobBody job)
    {
        if (!_receivedJobs.TryAdd(job.JobId, 0))
        {
            return false;
        }

        if (job.JobType == "PICKING")
        {
            _pickingJobs.Enqueue(job);
        }
        else
        {
            _storingJobs.Enqueue(job);
        }

        return true;
    }

    public bool TryGetPickingJob(out JobBody? job)
    {
        return _pickingJobs.TryDequeue(out job);
    }

    public bool TryGetStoringJob(out JobBody? job)
    {
        return _storingJobs.TryDequeue(out job);
    }
}
