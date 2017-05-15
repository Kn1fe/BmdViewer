using BmdViewer.PckEngine;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;

namespace BmdViewer
{
    public delegate void DSetProgressValue(int value);
    public delegate void DSetProgressMaximum(int value);

    public partial class MainWindow : Window
    {
        public static PckStream Buildings = new PckStream();
        ModelVisual3D model = new ModelVisual3D();

        public MainWindow()
        {
            InitializeComponent();
            ResetCamPos();
            Buildings.SetProgressMaximum += SetProgressMaximum;
            Buildings.SetProgressValue += SetProgressValue;
        }

        private void ResetCamPos()
        {
            Viewport.Camera.Position = new Point3D(0.0, 1.0, 4.5999999046325684);
            Viewport.Camera.LookDirection = new Vector3D(0.0, 0.0, -10.0);
            Viewport.Camera.UpDirection = new Vector3D(0.0, 1.0, 0.0);
            Viewport.LookAt(new Point3D(0.0, 1.0, 0.0), 4.0, 1000.0);
        }

        private void Render(string path)
        {
            BMDFile bmd = new BMDFile();
            bmd.Read(path);
            model.Content = bmd.GetModel();
            Viewport.Children.Add(model);
            ResetCamPos();
        }

        private void Viewport_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Render((e.Data.GetData(DataFormats.FileDrop) as string[]).First());
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ShowDialog();
            if (File.Exists(ofd.FileName))
                Buildings.Load(ofd.FileName);
        }

        void SetProgressValue(int value)
        {
            Progress.Dispatcher.BeginInvoke(new Action(() => Progress.Value = value));
        }

        void SetProgressMaximum(int value)
        {
            Progress.Dispatcher.BeginInvoke(new Action(() => Progress.Maximum = value));
        }
    }
}
