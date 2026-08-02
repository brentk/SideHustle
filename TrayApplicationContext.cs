public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _enableItem;
    private readonly WindowLayoutController _controller = new();

    private bool _enabled = true;

    public TrayApplicationContext()
    {
        _enableItem = new ToolStripMenuItem("Enable")
        {
            Checked = _enabled,
            CheckOnClick = true
        };

        _enableItem.CheckedChanged += (_, _) =>
        {
            _enabled = _enableItem.Checked;

            if (_enabled)
                StartWork();
            else
                StopWork();
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_enableItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, Exit);

        _icon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
            //Icon = new Icon("app.ico"),
            Text = "Window Manager",
            Visible = true,
            ContextMenuStrip = menu
        };

        StartWork();
    }

    private void StartWork()
    {
        _controller.Start();
    }

    private void StopWork()
    {
    
        _controller.Stop();
    }

    private void Exit(object? sender, EventArgs e)
    {
        StopWork();

        _controller.Dispose();
        _icon.Visible = false;
        _icon.Dispose();

        ExitThread();
    }
}
