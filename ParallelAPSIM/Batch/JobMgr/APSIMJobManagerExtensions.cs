﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Batch;
using Microsoft.WindowsAzure.Storage.Blob;
using ParallelAPSIM.APSIM;

namespace ParallelAPSIM.Batch.JobMgr
{
    public static class APSIMJobManagerExtensions
    {
        public static JobManagerTask ToJobManagerTask(this APSIMJob job, Guid jobId, CloudBlobClient blobClient, bool shouldSubmitTasks, bool autoScale)
        {
            var cmd = string.Format("cmd.exe /c {0} job-manager {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                BatchConstants.GetJobManagerPath(jobId),
                job.BatchCredentials.Url,
                job.BatchCredentials.Account,
                job.BatchCredentials.Key,
                job.StorageCredentials.Account,
                job.StorageCredentials.Key,
                jobId,
                BatchConstants.GetModelPath(jobId),
                shouldSubmitTasks,
                autoScale);

            return new JobManagerTask
            {
                CommandLine = cmd,
                DisplayName = "Job manager task",
                KillJobOnCompletion = true,
                Id = BatchConstants.JOB_MANAGER_NAME,
                RunExclusive = false,
                ResourceFiles = GetResourceFiles(job, blobClient).ToList(),
            };
        }

        private static IEnumerable<ResourceFile> GetResourceFiles(APSIMJob job, CloudBlobClient blobClient)
        {
            var toolsRef = blobClient.GetContainerReference("jobmanager");
            foreach (CloudBlockBlob listBlobItem in toolsRef.ListBlobs())
            {
                var sas = listBlobItem.GetSharedAccessSignature(new SharedAccessBlobPolicy
                {
                    SharedAccessStartTime = DateTime.UtcNow.AddHours(-1),
                    SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(2),
                    Permissions = SharedAccessBlobPermissions.Read,
                });
                yield return new ResourceFile(listBlobItem.Uri.AbsoluteUri + sas, listBlobItem.Name);
            }
        }
    }
}
