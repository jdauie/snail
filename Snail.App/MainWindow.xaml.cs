using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Snail;
using Snail.Nodes;

namespace Snail.App
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			string testFile;
			//testFile = @"..\Snail.Test\PsyStarcraft_channel.htm";
			//testFile = @"..\Snail.Test\kjv-osis\kjv.osis.xml";
			//testFile = @"..\Snail.Test\kjv-usfx\eng-kjv_usfx.xml";
			//testFile = @"..\Snail.Test\ot.xml";
			//testFile = @"..\Snail.Test\TO_core_last_DEM_AR_forests.shp.afe";
			//testFile = @"..\Snail.Test\xhtml1-strict.dtd";
			testFile = @"..\Snail.Test\Viewer3D.101.vcproj";
			string text = File.ReadAllText(testFile);

			var sw = Stopwatch.StartNew();

			DocumentNode root = null;
			for (int i = 0; i < 1; i++)
			{
				root = Document.Parse(text);
			}

			sw.Stop();

			var status = string.Format("Parsed {0} tags in {1} ms", (root != null) ? 1 : 0, sw.Elapsed.TotalMilliseconds);
			Trace.WriteLine(status);
			textBlock.Text = status;
		}
	}
}
