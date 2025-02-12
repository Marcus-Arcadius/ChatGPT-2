using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ChatGPT.Model.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChatGPT.ViewModels.Chat;

public class ChatMessageViewModel : ObservableObject
{
    private string? _role;
    private string? _message;
    private string? _format;
    private bool _isSent;
    private bool _isAwaiting;
    private bool _isError;
    private bool _canRemove;
    private bool _isEditing;
    private Func<ChatMessageViewModel, bool, Task>? _send;
    private Func<ChatMessageViewModel, Task>? _copy;
    private Action<ChatMessageViewModel>? _remove;

    [JsonConstructor]
    public ChatMessageViewModel()
    {
        AddCommand = new AsyncRelayCommand(async _ => await AddActionAsync());

        SendCommand = new AsyncRelayCommand(async _ => await SendActionAsync());

        EditCommand = new RelayCommand<string>(EditAction);

        CopyCommand = new RelayCommand(CopyAction);

        RemoveCommand = new RelayCommand(RemoveAction);

        SetRoleCommand = new RelayCommand<string>(SetRoleAction);

        SetFormatCommand = new RelayCommand<string>(SetFormatAction);

        OpenCommand = new AsyncRelayCommand(async _ => await OpenActionAsync());

        SaveCommand = new AsyncRelayCommand(async _ => await SaveActionAsync());
    }

    public ChatMessageViewModel(string role, string message) 
        : this()
    {
        _role = role;
        _message = message;
    }

    [JsonPropertyName("role")]
    public string? Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    [JsonPropertyName("message")]
    public string? Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    [JsonPropertyName("format")]
    public string? Format
    {
        get => _format;
        set => SetProperty(ref _format, value);
    }

    [JsonPropertyName("isSent")]
    public bool IsSent
    {
        get => _isSent;
        set => SetProperty(ref _isSent, value);
    }

    [JsonPropertyName("isAwaiting")]
    public bool IsAwaiting
    {
        get => _isAwaiting;
        set => SetProperty(ref _isAwaiting, value);
    }

    [JsonPropertyName("isError")]
    public bool IsError
    {
        get => _isError;
        set => SetProperty(ref _isError, value);
    }

    [JsonPropertyName("canRemove")]
    public bool CanRemove
    {
        get => _canRemove;
        set => SetProperty(ref _canRemove, value);
    }

    [JsonIgnore]
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    [JsonIgnore]
    public IAsyncRelayCommand AddCommand { get; }

    [JsonIgnore]
    public IAsyncRelayCommand SendCommand { get; }

    [JsonIgnore]
    public IRelayCommand EditCommand { get; }

    [JsonIgnore]
    public IRelayCommand CopyCommand { get; }

    [JsonIgnore]
    public IRelayCommand RemoveCommand { get; }

    [JsonIgnore]
    public IRelayCommand SetRoleCommand { get; }

    [JsonIgnore]
    public IRelayCommand SetFormatCommand { get; }

    [JsonIgnore]
    public IAsyncRelayCommand OpenCommand { get; }

    [JsonIgnore]
    public IAsyncRelayCommand SaveCommand { get; }

    private async Task AddActionAsync()
    {
        if (_send is { })
        {
            IsEditing = false;

            if (!IsSent)
            {
                await _send(this, true);
            }
        }
    }

    private async Task SendActionAsync()
    {
        if (_send is { })
        {
            IsEditing = false;

            if (!IsSent)
            {
                await _send(this, false);
            }
        }
    }

    private void EditAction(string? status)
    {
        switch (status)
        {
            case "Edit":
            {
                EditingState();
                break;
            }
            case "Cancel":
            {
                CanceledState();
                break;
            }
            case "NewLine":
            {
                NewLineState();
                break;
            }
        }
    }

    private void EditingState()
    {
        if (!IsEditing && IsSent)
        {
            IsEditing = true;
        }
    }

    private void CanceledState()
    {
        if (IsEditing)
        {
            IsEditing = false;
        }
    }

    private void NewLineState()
    {
        if (!IsSent)
        {
            // TODO: Use caret position to insert new line.
            Message += Environment.NewLine;
        }
    }

    private void CopyAction()
    {
        _copy?.Invoke(this);
    }

    private void RemoveAction()
    {
        _remove?.Invoke(this);
    }

    private void SetRoleAction(string? role)
    {
        if (role is { })
        {
            Role = role;
        }
    }

    private void SetFormatAction(string? format)
    {
        if (format is { })
        {
            Format = format;
        }
    }

    private async Task OpenActionAsync()
    {
        var app = Defaults.Locator.GetService<IApplicationService>();
        if (app is { })
        {
            await app.OpenFileAsync(
                OpenCallbackAsync, 
                new List<string>(new[] { "All" }), 
                "Open");
        }
    }

    private async Task SaveActionAsync()
    {
        var app = Defaults.Locator.GetService<IApplicationService>();
        if (app is { } && Message is { })
        {
            await app.SaveFileAsync(
                SaveCallbackAsync, 
                new List<string>(new[] { "Text", "All" }), 
                "Save", 
                "message",
                "txt");
        }
    }

    private async Task OpenCallbackAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var message = await reader.ReadToEndAsync();
        Message = message;
    }

    private async Task SaveCallbackAsync(Stream stream)
    {
        var message = Message;
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(message);
    }

    public void SetSendAction(Func<ChatMessageViewModel, bool, Task>? send)
    {
        _send = send;
    }

    public void SetCopyAction(Func<ChatMessageViewModel, Task>? copy)
    {
        _copy = copy;
    }

    public void SetRemoveAction(Action<ChatMessageViewModel>? remove)
    {
        _remove = remove;
    }

    public ChatMessageViewModel Copy()
    {
        var message = new ChatMessageViewModel
        {
            Role = _role,
            Message = _message,
            Format = _format,
            IsSent = _isSent,
            IsAwaiting = _isAwaiting,
            IsError = _isError,
            CanRemove = _canRemove,
            IsEditing = _isEditing
        };

        message.SetSendAction(_send);
        message.SetCopyAction(_copy);
        message.SetRemoveAction(_remove);

        return message;
    }
}
