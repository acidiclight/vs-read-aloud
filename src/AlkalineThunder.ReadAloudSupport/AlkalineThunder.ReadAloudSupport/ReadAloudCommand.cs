using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace AlkalineThunder.ReadAloudSupport
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ReadAloudCommand
    {
        private TtsManager _tts = new TtsManager();

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("fe6c02f6-b7c4-4ca6-b0cc-7208869f9d55");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadAloudCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ReadAloudCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);

            menuItem.BeforeQueryStatus += HandleBeforeQueryStatus;

            commandService.AddCommand(menuItem);
        }

        private void HandleBeforeQueryStatus(object sender, EventArgs e)
        {
            // So the logic here is pretty much identical to the Rider version of this plugin.
            // We need to check if there's an active editor. If there is no active editor, then
            // we must completely hide the menu command.
            //
            // If there is an editor, but there is no  text selection, then we must make the command
            // visible but grey it out in the menu.
            //
            // So let's do it.

            // First grab the menu command.
            var menuCommand = (OleMenuCommand)sender;

            // Now let's check to see if there's an editor.
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE)package.GetServiceAsync(typeof(DTE)).GetAwaiter().GetResult();

            // If that returned null, what the hell kind of IDE ARE WE USING!? Definitely hide the menu.
            // If the DTE reports no active document, do the same
            if (dte == null || dte.ActiveDocument == null)
            {
                menuCommand.Visible = false;
                return;
            }

            // We can make it visible.
            menuCommand.Visible = true;

            // Check if there's a valid text selection.
            var selection = (TextSelection)dte.ActiveDocument.Selection;
            if (selection  != null && !string.IsNullOrWhiteSpace(selection.Text))
            {
                menuCommand.Enabled = true;
            }
            else
            {
                menuCommand.Enabled = false;
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ReadAloudCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in ReadAloudCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ReadAloudCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Retrieve the selected text.
            var selectedText = GetSelectedTextInEditorAsync().GetAwaiter().GetResult();

            // Fucking read it lol.
            _tts.PrepareSpeech(selectedText);
        }

        private async Task<string> GetSelectedTextInEditorAsync()
        {
            var dte = (DTE) (await this.package.GetServiceAsync(typeof(DTE)));

            // Null-checks
            if (dte == null || dte.ActiveDocument == null)
                return string.Empty;

            // Retrieve the active document selection.
            var selection = (TextSelection)dte.ActiveDocument.Selection;

            // Return the text.
            return selection.Text;
        }
    }
}
