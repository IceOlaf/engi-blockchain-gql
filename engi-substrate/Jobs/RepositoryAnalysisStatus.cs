namespace Engi.Substrate.Jobs;

public enum RepositoryAnalysisStatus
{
    Created = 0,
    Queued = 1,
    Started = 2,
    Completed = 10,
    Failed = 11
}