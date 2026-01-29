using System;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nolvus.Core.Services;

namespace Nolvus.Dashboard.Forms
{
    public partial class NolvusInstanceTag : Window
    {
        private static readonly Regex _invalidCharRegex = new Regex(@"[^a-zA-Z0-9\s]", RegexOptions.Compiled);

        public NolvusInstanceTag()
        {
            InitializeComponent();
            Opened += (_, __) => TxtBxTag.Focus();
        }

        public NolvusInstanceTag(string title) : this()
        {
            Title = title;
        }

        public string InstanceTag => (TxtBxTag.Text ?? string.Empty).Trim();

        public static NolvusInstanceTag EnterTag(string title) => new NolvusInstanceTag(title);

        private void BtnOK_Click(object? sender, RoutedEventArgs e)
        {
            LblError.IsVisible = false;

            var tag = InstanceTag;
            if (string.IsNullOrWhiteSpace(tag))
            {
                ShowError("You must enter a tag!");
                return;
            }

            var working = ServiceSingleton.Instances.WorkingInstance;
            if (working != null && ServiceSingleton.Instances.InstanceExists(working.Name, tag))
            {
                ShowError($"The Tag {tag} already exists for instance {working.Name}");
                return;
            }

            Close(true);
        }

        private void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private void TxtBxTag_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnOK_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                BtnCancel_Click(sender, e);
                e.Handled = true;
            }
        }

        private void TxtBxTag_TextChanging(object? sender, TextChangingEventArgs e)
        {
            if (sender is not TextBox tb)
                return;

            var text = tb.Text ?? string.Empty;

            if (_invalidCharRegex.IsMatch(text))
            {
                var cleaned = _invalidCharRegex.Replace(text, string.Empty);

                var caret = tb.CaretIndex;
                tb.Text = cleaned;
                tb.CaretIndex = Math.Min(caret - 1, cleaned.Length);
            }
        }

        private void ShowError(string message)
        {
            LblError.Text = message;
            LblError.IsVisible = true;
        }
    }
}
