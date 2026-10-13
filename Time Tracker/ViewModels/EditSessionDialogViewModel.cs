using System;
using Time_Tracker.Models;

namespace Time_Tracker.ViewModels;

public class EditSessionDialogViewModel : ViewModelBase
{
    private string _activityName = string.Empty;
    private string _startText = string.Empty;
    private string _endText = string.Empty;
    private string _notes = string.Empty;
    private string _errorMessage = string.Empty;

    public EditSessionDialogViewModel(EditSessionModel model)
    {
        SourceModel = model;
        _activityName = model.ActivityName;
        _startText = model.StartLocal.ToString("yyyy-MM-dd HH:mm:ss");
        _endText = model.EndLocal?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
        _notes = model.Notes;
    }

    public EditSessionModel SourceModel { get; }

    public string ActivityName
    {
        get => _activityName;
        set => SetProperty(ref _activityName, value);
    }

    public string StartText
    {
        get => _startText;
        set => SetProperty(ref _startText, value);
    }

    public string EndText
    {
        get => _endText;
        set => SetProperty(ref _endText, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool TryBuildResult(out EditSessionModel? result)
    {
        result = null;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(ActivityName))
        {
            ErrorMessage = "Activity name is required.";
            return false;
        }

        if (!DateTime.TryParse(StartText, out var startLocal))
        {
            ErrorMessage = "Start date/time is invalid.";
            return false;
        }

        DateTime? endLocal = null;
        if (!string.IsNullOrWhiteSpace(EndText))
        {
            if (!DateTime.TryParse(EndText, out var parsedEnd))
            {
                ErrorMessage = "End date/time is invalid.";
                return false;
            }

            endLocal = parsedEnd;
        }

        if (endLocal is not null && endLocal < startLocal)
        {
            ErrorMessage = "End date/time must be after start date/time.";
            return false;
        }

        result = new EditSessionModel
        {
            ActivityId = SourceModel.ActivityId,
            TimeEntryId = SourceModel.TimeEntryId,
            ActivityName = ActivityName.Trim(),
            StartLocal = DateTime.SpecifyKind(startLocal, DateTimeKind.Local),
            EndLocal = endLocal is null ? null : DateTime.SpecifyKind(endLocal.Value, DateTimeKind.Local),
            Notes = Notes.Trim(),
        };

        return true;
    }
}
