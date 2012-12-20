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
	public static class Commands
	{
		static Commands()
		{
			CloseWindow = NewCommand("Close Window", "CloseWindow", new KeyGesture(Key.Escape));
			CloseWindowDefaultBinding = new CommandBinding(CloseWindow, CloseWindowExecute, CloseWindowCanExecute);
		}

		static RoutedUICommand NewCommand(string text, string name, KeyGesture gesture)
		{
			return new RoutedUICommand(text, name, typeof(System.Windows.Window), new InputGestureCollection { gesture });
		}

		public static CommandBinding CloseWindowDefaultBinding { get; private set; }
		public static RoutedUICommand CloseWindow { get; private set; }

		static void CloseWindowCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = sender != null && sender is System.Windows.Window;
			e.Handled = true;
		}
		static void CloseWindowExecute(object sender, ExecutedRoutedEventArgs e)
		{
			 ((System.Windows.Window)sender).Close();
		}
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			ParseTest();
			//CompareProjectFilesTest();

			CommandBindings.Add(Commands.CloseWindowDefaultBinding);
		}

		private void ParseTest()
		{
			string testFile;
			
			//testFile = @"..\Snail.Test\html\PsyStarcraft_channel.htm";
			//testFile = @"..\Snail.Test\html\stackoverflow.htm";
			//testFile = @"..\Snail.Test\html\slashdot.htm";
			//testFile = @"..\Snail.Test\html\text.htm";
			//testFile = @"..\Snail.Test\html\xhtml1-strict.dtd";

			testFile = @"..\Snail.Test\kjv-osis\kjv.osis.xml";
			//testFile = @"..\Snail.Test\kjv-usfx\eng-kjv_usfx.xml";
			//testFile = @"..\Snail.Test\religion-1.02\ot.xml";

			//testFile = @"..\Snail.Test\pugixml-benchmark-data\house.dae";
			//testFile = @"..\Snail.Test\pugixml-benchmark-data\terrover.xml";

			//testFile = @"..\Snail.Test\afe\TO_core_last_DEM_AR_forests.shp.afe";
			//testFile = @"..\Snail.Test\afe\Precompaction_Lrn.shp.afe";

			//testFile = @"..\Snail.Test\vcproj\Viewer3D.101.vcproj";

			//testFile = @"..\Snail.Test\xmark\standard.xml";

			string text = File.ReadAllText(testFile);

			GC.Collect();
			GC.Collect();

			var sw = Stopwatch.StartNew();

			DocumentNode root = Document.Parse(text);

			sw.Stop();

			if (root != null)
				treeView1.ItemsSource = root.Children;

			var status = string.Format("Parsed {0} tags in {1} ms", (root != null) ? 1 : 0, sw.Elapsed.TotalMilliseconds);
			Trace.WriteLine(status);
			textBlock.Text = status;
		}

		private void CompareProjectFilesTest()
		{
			CompareProjectFiles(
				@"..\Snail.Test\vcproj\Viewer3D.101.vcproj",
				@"..\Snail.Test\vcproj\Viewer3D.102.vcproj"
				);
		}

		private void CompareProjectFiles(string p1, string p2)
		{
			var text1 = File.ReadAllText(p1);
			var root1 = Document.Parse(text1);
			root1.Sort();
			//File.WriteAllText(@"..\Snail.Test\compare1.txt", root1.ToFormattedString(WhitespaceMode.Insert, "\t"));

			var text2 = File.ReadAllText(p2);
			var root2 = Document.Parse(text2);
			//File.WriteAllText(@"..\Snail.Test\compare2.txt", root2.ToFormattedString(WhitespaceMode.Insert, "\t"));

			// compare trees for differences (not considering order)


			treeView1.ItemsSource = root1.Children;

			var status = string.Format("Compared {0}, {1}", System.IO.Path.GetFileName(p1), System.IO.Path.GetFileName(p2));
			Trace.WriteLine(status);
			textBlock.Text = status;
		}
	}
}
