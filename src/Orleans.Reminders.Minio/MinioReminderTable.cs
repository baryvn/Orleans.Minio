﻿
using Orleans.Runtime;

namespace Orleans.Reminders.Minio
{
    public class MinioReminderTable : IReminderTable
    {
        public Task Init()
        {
            throw new NotImplementedException();
        }

        public Task<ReminderEntry> ReadRow(GrainId grainId, string reminderName)
        {
            throw new NotImplementedException();
        }

        public Task<ReminderTableData> ReadRows(GrainId grainId)
        {
            throw new NotImplementedException();
        }

        public Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag)
        {
            throw new NotImplementedException();
        }

        public Task TestOnlyClearTable()
        {
            throw new NotImplementedException();
        }

        public Task<string> UpsertRow(ReminderEntry entry)
        {
            throw new NotImplementedException();
        }
    }
}
