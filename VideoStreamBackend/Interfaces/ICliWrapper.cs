using CliWrap;
using CliWrap.Buffered;

namespace VideoStreamBackend.Interfaces;

public interface ICliWrapper
{
    // Command Wrap(string command);

    Task<CommandResult> ExecuteBufferedAsync(string targetFilePath, string argument, PipeTarget standardOutput, PipeTarget errorOutput);
}

public class CliWrapper : ICliWrapper
{
    // public Command Wrap(string command) {
    //     var result = Cli.Wrap(command);
    //     
    //     return new CommandResult {
    //         IsSuccess = result.
    //     };
    // }

    public async Task<CommandResult> ExecuteBufferedAsync(string targetFilePath, string argument, PipeTarget standardOutput, PipeTarget standardError) {
        var result = await Cli.Wrap(targetFilePath)
            .WithArguments(argument)
            .WithStandardOutputPipe(standardOutput)
            .WithStandardErrorPipe(standardError)
            .ExecuteBufferedAsync();

        return new CommandResult {
            IsSuccess = result.IsSuccess
        };
    }
}

public class CommandResult {
    public bool IsSuccess;
}
