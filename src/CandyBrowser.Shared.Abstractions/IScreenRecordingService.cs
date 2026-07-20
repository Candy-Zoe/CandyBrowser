namespace CandyBrowser.Shared.Abstractions;

/// <summary>
/// Service for recording WebView2 content.
/// </summary>
public interface IScreenRecordingService
{
    bool IsRecording { get; }
    Task<bool> StartRecordingAsync(int fps = 15, int durationSeconds = 60);
    void StopRecording();
    string? LastRecordingPath { get; }
}
