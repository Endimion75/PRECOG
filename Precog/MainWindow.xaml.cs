using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Common;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using DataModels;
using Precog.Controls;
using Precog.DialogWindows;
using Precog.Utils;
using Visiblox.Charts.Primitives;

namespace Precog
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region --- CloseCommand ---

        private Utils.RelayCommand _cmdCloseCommand;
        /// <summary>
        /// Returns a command that closes a TabItem.
        /// </summary>
        public ICommand CloseCommand
        {
            get
            {
                if (_cmdCloseCommand == null)
                {
                    _cmdCloseCommand = new Utils.RelayCommand(
                        param => this.CloseTab_Execute(param),
                        param => this.CloseTab_CanExecute(param)
                        );
                }
                return _cmdCloseCommand;
            }
        }

        private void CloseTab_Execute(object parm)
        {
            TabItem ti = parm as TabItem;
            if (ti != null)
                ViewTab.Items.Remove(parm);
        }

        private bool CloseTab_CanExecute(object parm)
        {
            TabItem ti = parm as TabItem;
            if (ti != null && ti != ViewTab.Items[0])
                return ti.IsEnabled;

            return false;
        }

        #endregion

        private List<ExperimentalRun> _experimentRuns = new List<ExperimentalRun>();
        public List<ExperimentalRun> ExperimentalRuns
        {
            get { return _experimentRuns; }
            set { _experimentRuns = value; }
        }

        private List<ExperimentalRun> _originalExperientalRuns = new List<ExperimentalRun>(); 

        private ExperimentalRun _selectedRun;

        private ExperimentType _experimentViewType;

        private bool _useFirstDerivative = false;

        private bool _displayQIndexes = false;

        internal delegate void EmptyDelegate();

        private GridViewColumnHeader _curSortCol = null;
        private SortAdorner _curAdorner = null;

        private ListSortDirection _lstViewDirection = ListSortDirection.Ascending;
 
        public class SortAdorner : Adorner
        {
            private readonly static Geometry _AscGeometry =
               Geometry.Parse("M 0,0 L 10,0 L 5,5 Z");

            private readonly static Geometry _DescGeometry =
                Geometry.Parse("M 0,5 L 10,5 L 5,0 Z");

            public ListSortDirection Direction { get; private set; }

            public SortAdorner(UIElement element, ListSortDirection dir)
                : base(element)
            { Direction = dir; }

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);

                if (AdornedElement.RenderSize.Width < 20)
                    return;

                drawingContext.PushTransform(
                     new TranslateTransform(
                       AdornedElement.RenderSize.Width - 15,
                      (AdornedElement.RenderSize.Height - 5) / 2));

                drawingContext.DrawGeometry(Brushes.Black, null,
                    Direction == ListSortDirection.Ascending ?
                      _AscGeometry : _DescGeometry);

                drawingContext.Pop();
            }
        }

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                this.DataContext = this;
                this.Resources.Add("ExperimentRuns", _experimentRuns);
                Ini();
                
            }
            catch (Exception ex )
            {
                MessageBox.Show(ex.Message,"Ini Error",MessageBoxButton.OK,MessageBoxImage.Error);
                throw;
            }
        }

        private void Ini()
        {
            ImportBSControl.BSFilesList.Items.Clear();
            PopulateListViewViews();
        }

        private void PopulateListViewViews()
        {
            if (lstView != null)
            {
                lstView.Items.Add(new ComboBoxItem { Content = "Table", Tag = "GridView" });
                lstView.Items.Add(new ComboBoxItem { Content = "Thumbnail", Tag = "ImageView" });
                lstView.Items.Add(new ComboBoxItem { Content = "Detailed Thumbnail", Tag = "ImageDetailView" });
                lstView.SelectedIndex = 0;
            }
        }

        private void SortHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = sender as GridViewColumnHeader;
            String field = column.Tag as String;

            if (_curSortCol != null)
            {
                var adorner = AdornerLayer.GetAdornerLayer(_curSortCol);
                if (adorner != null)
                    adorner.Remove(_curAdorner);
                lstViewExperiments.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (_curSortCol == column && _curAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            _curSortCol = column;
            _curAdorner = new SortAdorner(_curSortCol, newDir);
            AdornerLayer.GetAdornerLayer(_curSortCol).Add(_curAdorner);
            if (field == "ContainerName")
                field = "ContainerIndex";

            lstViewExperiments.Items.SortDescriptions.Add(new SortDescription(field, newDir));
        }
 
        private void lstView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateListView();

            if (ckDisplayRaw != null)
                ckDisplayRaw.Visibility = lstView.SelectedIndex == 0 ? Visibility.Collapsed : Visibility.Visible;
            if (ckShowFirstDerivative != null)
                ckShowFirstDerivative.Visibility = lstView.SelectedIndex == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ExperimentalRunList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRun = (ExperimentalRun) ExperimentalRunList.SelectedItem;
            ckDisplayRaw.IsChecked = true;
            ProccessReplicates();
        }

        private void UpdateListView()
        {
            var selectedItem = (ComboBoxItem)lstView.SelectedItem;
            var qIdxTag = _displayQIndexes ? "QIdx" : "";
            
            if (selectedItem == null) return;
            if ((string)selectedItem.Tag == "ImageView" || (string)selectedItem.Tag == "ImageDetailView")
                lstViewExperiments.View = (ViewBase)FindResource(selectedItem.Tag);
            else if ((string) selectedItem.Tag == "GridView" && _experimentViewType == ExperimentType.Samples)
                lstViewExperiments.View = (ViewBase) FindResource("GridViewSamples" + qIdxTag);
            else
                lstViewExperiments.View = (ViewBase) FindResource(selectedItem.Tag + qIdxTag);
        }

       private void ProccessReplicates()
        {
            if (_selectedRun == null)
                return;

            var originalRun = _originalExperientalRuns.Where(e => e.ImportFileName == _selectedRun.ImportFileName).First();
            var selectedRun = CloneExperimentalRun(originalRun);
            UpdateListView();
            BindExperimentalRun(selectedRun);
        }


        private void BindExperimentalRun(ExperimentalRun experimentalRun)
        {
            var newThread = new Thread(CreateRunView);
            newThread.SetApartmentState(ApartmentState.STA);
            newThread.Start(experimentalRun);
            
        }

        #region ProgressBar
        private void InitializeProgressBar(int maximum)
        {
            pBarView.Minimum = 0;
            pBarView.Maximum = maximum;
            pBarView.Value = 0;
            pBarView.Visibility = Visibility.Visible;
            ProgressCount.Visibility = Visibility.Visible;
        }

        private void AdvanceProgressBar()
        {
            pBarView.Value += 1;
        }

        private void SetIndeterminateProgressBar(bool isIndeterminate)
        {
            pBarView.Visibility = isIndeterminate ? Visibility.Visible : Visibility.Hidden;
            pBarView.IsIndeterminate = isIndeterminate;
        }

        private void UpdateProgressText(string message)
        {
            ProgressText.Text = message;
        }
        #endregion

        private void CreateRunView(object paramter)
        {
            var experimentalRun = (ExperimentalRun)paramter;

            var viewRun = CloneExperimentalRun(experimentalRun);
            viewRun.Run = new RelayingObservableCollection<Culture>();

            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
            {
                UpdateProgressText("Preparing data for  " + experimentalRun.Run.Count().ToString() + " elements ...");
                ViewTab.SelectedIndex = 0;

                InitializeProgressBar(experimentalRun.Run.Count());

                BlockUI(true);
                lstViewExperiments.DataContext = viewRun;
            });
            
            foreach (var culture in experimentalRun.Run)
            {
                Culture culture1 = culture;
                this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
                {
                    UpdateProgressText("Creating garph for: " + culture1.Container);
                });
                this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
                {
                    var validTypes = new List<DataType>();
                    if (_useFirstDerivative)
                        validTypes.Add(DataType.FirstDerivative);
                    else
                    {
                        if((bool) ckDisplayRaw.IsChecked)
                             validTypes.Add(DataType.Raw);
                        validTypes.Add(DataType.Processed);
                    }
                   var img = ChartHelper.CreateChartImage(experimentalRun, culture1.ContainerIndex, validTypes, 80, 80, 0, true);
                   culture1.GraphImageSource = img;
                });
                this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
                {
                    viewRun.Run.Add(culture1);
                });
                this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)AdvanceProgressBar);
            }
            
            experimentalRun.Run = viewRun.Run;

            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate()
            {
                
                BlockUI(false);
                UpdateProgressText("");
                var selectedRun = _experimentRuns.Where(e => e.ImportFileName == viewRun.ImportFileName).First();
                var warnings = selectedRun.Run.Count(p=>p.QualityIndex.HasFlags);
                if(warnings >0)
                    UpdateProgressText("PRECOG detected qualitity warnings (flags) for "+ warnings+" wells!");
                selectedRun.Run = viewRun.Run;
            });
        }

        private void BlockUI(bool block)
        {
            RootLayout.IsEnabled = !block;
            Mouse.OverrideCursor = block ? Cursors.Wait : Cursors.Arrow;
            if (!block)
            {
                var itemsTab = (TabControl)FindName("ViewTab");
                foreach (TabItem item in itemsTab.Items)
                    item.IsEnabled = true;
            }
        }

        private void lstViewExperiments_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listView = sender as ItemsControl;
            var originalSender = e.OriginalSource as DependencyObject;
            if (listView == null || originalSender == null || _useFirstDerivative) return;

            DependencyObject container =
                ItemsControl.ContainerFromElement
                (sender as ItemsControl, e.OriginalSource as DependencyObject);

            if (container == null ||
                container == DependencyProperty.UnsetValue) return;

            // found a container, now find the item.
            object activatedItem =
                listView.ItemContainerGenerator.ItemFromContainer(container);

            if (activatedItem != null)
            {
                var experimentalRun = (ExperimentalRun)((ListView) sender).DataContext;
                //var selectedItemIndex = ((ListView) sender).SelectedItem; // .SelectedIndex;
                var selectedCulture = (Culture) ((ListView) sender).SelectedItem;
                var cultureIndex = experimentalRun.Run.IndexOf(selectedCulture);
                CreateZoomTab(cultureIndex, experimentalRun);
            }
        }

        #region CreateZoomTab

        private void CreateZoomTab(int cultureIndex, ExperimentalRun selectedExperimentalRun)
        {
            var displayItems = new List<int> {cultureIndex};
            CreateTab(displayItems, selectedExperimentalRun);
        }

        private void CreateZoomTab(List<int> displayItems, ExperimentalRun selectedExperimentalRun)
        {
            CreateTab(displayItems, selectedExperimentalRun);
        }

        private void CreateTab(List<int> displayItems, ExperimentalRun selectedExperimentalRun)
        {
            var itemsTab = (TabControl) FindName("ViewTab");
            ResetHaveFocusOnAllTabs(itemsTab);

            var cultureIndex = displayItems.First();
            string container = displayItems.Count == 1 ? selectedExperimentalRun.Run[cultureIndex].Container : GetSampelsName(displayItems, selectedExperimentalRun);
            var runName = selectedExperimentalRun.ImportFileName;
            var newTab = new TabItem { Header = container + " @ " + runName };
            newTab.MouseUp += new MouseButtonEventHandler(TabItem_Click);

            var neuralParameters = new Macro.NeuralParameters
                                       {
                                           Iterations = ProcessDataControl.iterations,
                                           LearningRate = ProcessDataControl.learningRate,
                                           NeuronsInFirstLayer = ProcessDataControl.neuronsInFirstLayer,
                                           SigmoidAlphaValue = ProcessDataControl.sigmoidAlphaValue
                                       };

            var graph = new zoomGraph
                            {
                                ExperimentalRun = selectedExperimentalRun,
                                NeuralParameters = neuralParameters
                            };
            
            var zgcv = new ZoomGraphControlValues
                           {
                               DisplayRaw = false,
                               DisplayFD = false,
                               DisplayMetaLagData = false,
                               DisplayMetaRateData = false,
                               DisplayMetaYieldData = false,
                               LogYAxis = true,
                               ChartBehaviour = ChartBehaviour.Zoom,
                               FitData = false,
                               EnableFit = false,
                               SkipPointsNumber = 0
                           };

            ZoomGraphControlsControl.ControlValues = zgcv;
            graph.ZoomGraphControlValues = zgcv;

            SetZoomGraphBindings(graph);

            graph.DisplayFirstDervivative = false;

            graph.HasFocus = true;

            graph.DisplayItems = displayItems;
            
            graph.SeriesCreated += new RoutedEventHandler(GraphSeries_Created);
            graph.NameChanged += new RoutedEventHandler(NameChanged_Executed);

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition {Height = GridLength.Auto});
            grid.RowDefinitions.Add(new RowDefinition());
            Grid.SetRow(graph,1);
            grid.Children.Add(graph);

            newTab.Content = grid;

            if (itemsTab != null)
            {
                itemsTab.Items.Add(newTab);
                itemsTab.SelectedItem = newTab;
            }

            ActionsExpander.IsExpanded = true;
            GraphControlsExpander.IsExpanded = true;
        }

        private static void ResetHaveFocusOnAllTabs(TabControl itemsTab)
        {
            foreach (TabItem item in itemsTab.Items)
            {
                if (((Grid) item.Content).Children[0].GetType() == typeof (zoomGraph))
                {
                    var zoomGraph = (zoomGraph) ((Grid) item.Content).Children[0];
                    zoomGraph.HasFocus = false;
                }
            }
        }

        private void SetZoomGraphBindings(zoomGraph graph)
        {
            var zoomGraphControlValues_rbRawBinding = new Binding();
            zoomGraphControlValues_rbRawBinding.Source = ZoomGraphControlsControl.ckDisplayRaw;
            zoomGraphControlValues_rbRawBinding.Path = new PropertyPath("IsChecked");
            graph.SetBinding(zoomGraph.ControlDisplayRawProperty, zoomGraphControlValues_rbRawBinding);

            var zoomGraphControlValues_rbFDBinding = new Binding();
            zoomGraphControlValues_rbFDBinding.Source = ZoomGraphControlsControl.ckDisplayFD;
            zoomGraphControlValues_rbFDBinding.Path = new PropertyPath("IsChecked");
            graph.SetBinding(zoomGraph.ControlDisplayFDProperty, zoomGraphControlValues_rbFDBinding);

            var zoomGraphControlValues_ckDisplayMetaDataLagBinding = new Binding();
            zoomGraphControlValues_ckDisplayMetaDataLagBinding.Source = ZoomGraphControlsControl.ckDisplayMetaDataLag;
            zoomGraphControlValues_ckDisplayMetaDataLagBinding.Path = new PropertyPath("IsChecked");
            graph.SetBinding(zoomGraph.ControlDisplayMetaLagDataProperty, zoomGraphControlValues_ckDisplayMetaDataLagBinding);

            var zoomGraphControlValues_ckDisplayMetaDataRateBinding = new Binding();
            zoomGraphControlValues_ckDisplayMetaDataRateBinding.Source = ZoomGraphControlsControl.ckDisplayMetaDataRate;
            zoomGraphControlValues_ckDisplayMetaDataRateBinding.Path = new PropertyPath("IsChecked");
            graph.SetBinding(zoomGraph.ControlDisplayMetaRateDataProperty, zoomGraphControlValues_ckDisplayMetaDataRateBinding);

            var zoomGraphControlValues_ckDisplayMetaDataYieldBinding = new Binding();
            zoomGraphControlValues_ckDisplayMetaDataYieldBinding.Source = ZoomGraphControlsControl.ckDisplayMetaDataYield;
            zoomGraphControlValues_ckDisplayMetaDataYieldBinding.Path = new PropertyPath("IsChecked");
            graph.SetBinding(zoomGraph.ControlDisplayMetaYieldDataProperty, zoomGraphControlValues_ckDisplayMetaDataYieldBinding);

            var zoomGraphControlValues_ckLogYAxisBinding = new Binding();
            zoomGraphControlValues_ckLogYAxisBinding.Source = ZoomGraphControlsControl.ckLogYAxis;
            zoomGraphControlValues_ckLogYAxisBinding.Path = new PropertyPath("IsChecked");
            graph.SetBinding(zoomGraph.ControlLogYAxisProperty, zoomGraphControlValues_ckLogYAxisBinding);

            var zoomGraphControlValues_rbZoomBinding = new Binding();
            zoomGraphControlValues_rbZoomBinding.Source = ZoomGraphControlsControl.rbZoom;
            zoomGraphControlValues_rbZoomBinding.Path = new PropertyPath("IsChecked");
            graph.SetBinding(zoomGraph.ControlZoomProperty, zoomGraphControlValues_rbZoomBinding);

            var zoomGraphControlValues_rbPanBinding = new Binding();
            zoomGraphControlValues_rbPanBinding.Source = ZoomGraphControlsControl.rbPan;
            zoomGraphControlValues_rbPanBinding.Path = new PropertyPath("IsChecked");
            graph.SetBinding(zoomGraph.ControlPanProperty, zoomGraphControlValues_rbPanBinding);

            var zoomGraphControlValues_btnFitBinding = new Binding();
            zoomGraphControlValues_btnFitBinding.Source = ZoomGraphControlsControl.btnFitData;
            zoomGraphControlValues_btnFitBinding.Path = new PropertyPath("Tag");
            graph.SetBinding(zoomGraph.ControlFitDataProperty, zoomGraphControlValues_btnFitBinding);

            var zoomGraphControlValues_btnZoomFit = new Binding();
            zoomGraphControlValues_btnZoomFit.Source = ZoomGraphControlsControl.btnZoomFit;
            zoomGraphControlValues_btnZoomFit.Path = new PropertyPath("Tag");
            graph.SetBinding(zoomGraph.ControlZoomFitProperty, zoomGraphControlValues_btnZoomFit);

            var zoomGraphControlValues_btnFitEnableBinding = new Binding();
            zoomGraphControlValues_btnFitEnableBinding.Source = ZoomGraphControlsControl.btnFitData;
            zoomGraphControlValues_btnFitEnableBinding.Path = new PropertyPath("IsEnabled");
            zoomGraphControlValues_btnFitEnableBinding.Mode = BindingMode.OneWayToSource;
            graph.SetBinding(zoomGraph.ControlEnableFitDataProperty, zoomGraphControlValues_btnFitEnableBinding);

        }

        private void TabItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.Source.GetType() == typeof(TabItem)) 
            {
                if (((Grid)((TabItem)sender).Content).Children[0].GetType() == typeof(zoomGraph))
                {
                    var itemsTab = (TabControl)FindName("ViewTab");
                    ResetHaveFocusOnAllTabs(itemsTab);
                    
                    var zoomGraph = (zoomGraph)((Grid)((TabItem)sender).Content).Children[0];
                    zoomGraph.HasFocus = true;
                    ZoomGraphControlsControl.ControlValues = null;
                    ZoomGraphControlsControl.ControlValues = zoomGraph.ZoomGraphControlValues;
                    zoomGraph.RefreshZoomGraph();
                }
            }
        }

        private static string GetSampelsName(List<int> displayItems, ExperimentalRun selectedExperimentalRun)
        {
            var wells = new StringBuilder();
            foreach (var displayItem in displayItems)
            {
                var culture = selectedExperimentalRun.Run[displayItem];
                BuildWellString(culture, ref wells);
            }
            wells.Remove(wells.Length - 1, 1);
            wells.Append(")");
            string sampelsName = "(" + wells.ToString();
            return sampelsName;
        }

        private static void BuildWellString(Culture culture, ref StringBuilder wells)
        {
            if (wells.Length < 7)
                wells.Append(culture.ContainerIndex.ToString()).Append(",");
            else if (wells.Length == 8)
                wells.Remove(wells.Length - 1, 1).Append("...");
        }

        void GraphSeries_Created(object sender, RoutedEventArgs e)
        {
            UpdateProgressText((string)e.Source);
            DoEvents();
        }

        void NameChanged_Executed(object sender, RoutedEventArgs e)
        {
            var itemsTab = (TabControl)FindName("ViewTab");
            if (itemsTab != null)
            {
                var x = (TabItem)itemsTab.SelectedItem;
                if (x.Name != "Default")
                { 
                    var header = x.Header.ToString().Split(Convert.ToChar("@"));
                    x.Header = string.Format(CultureInfo.InvariantCulture, "{0} @ {1}", e.OriginalSource.ToString().Trim(), header[1].Trim());
                }
                
            }
        }

        #endregion

        private void pBarView_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (pBarView.Value == pBarView.Maximum)
            {
                pBarView.Visibility = Visibility.Hidden;
                ProgressCount.Visibility = Visibility.Hidden;
                ProgressText.Text = string.Empty;
            }
                
        }

        #region ImportBSControl

        private void ImportBSControl_ImportStep(object sender, RoutedEventArgs e)
        {
            AdvanceProgressBar();
            DoEvents();
        }

        private void ImportBSControl_ImportStarted(object sender, RoutedEventArgs e)
        {
            BlockUI(true);
            var maximum = (int) e.OriginalSource;
            InitializeProgressBar(maximum);
        }

        private void ImportBSControl_ImportEnded(object sender, RoutedEventArgs e)
        {
            _originalExperientalRuns = (List<ExperimentalRun>)e.OriginalSource;
            _experimentRuns = CloneExperimentalRunList(_originalExperientalRuns);

            ExperimentalRunList.ItemsSource = _experimentRuns;
            if (_originalExperientalRuns.Any())
            {
                UploadExpander.IsExpanded = false;
                ActionsExpander.IsExpanded = false;
            }
            BlockUI(false);
            ResetTabControl();
            lstViewExperiments.DataContext = null;
        }

        private void ProcessDataControl_OnParamaterSet(object sender, RoutedEventArgs e)
        {
            TransformDataExpander.IsExpanded = false;
            UploadExpander.IsExpanded = true;
            UploadExpander.BringIntoView();
            //scrlActions.ScrollToTop();
        }



        private void ResetTabControl()
        {
            var itemsTab = (TabControl) FindName("ViewTab");
            var count = itemsTab.Items.Count;
            for (int i = 1; i < count; i++)
            {
                itemsTab.Items.RemoveAt(1);
            }
        }

        private List<ExperimentalRun> CloneExperimentalRunList(List<ExperimentalRun> listToclone)
        {
            var clone = new List<ExperimentalRun>();
            foreach (var run in listToclone)
            {
                var newExp = CloneExperimentalRun(run);
                clone.Add(newExp);
            }
            return clone;
        }

        private static ExperimentalRun CloneExperimentalRun(ExperimentalRun run)
        {
            var newExp = new ExperimentalRun(run.ImportFileName)
                             {
                                 Name = run.Name,
                                 CreationDate = run.CreationDate,
                                 ExperimentType = run.ExperimentType,
                                 FileType = run.FileType,
                                 ReferenceCultureIndex = run.ReferenceCultureIndex,
                                 ReplicateBehaviour = run.ReplicateBehaviour,
                                 RunODRange = run.RunODRange,
                                 RunTimeRange = run.RunTimeRange
                             };
            foreach (var culture in run.Run)
                newExp.Run.Add(culture);
            return newExp;
        }

        private void ImportBSControl_ImportMessage(object sender, RoutedEventArgs e)
        {
            var message = (string)e.OriginalSource;
            UpdateProgressText(message);
            DoEvents();
        }

        #endregion

       private void ckShowFirstDerivative_Click(object sender, RoutedEventArgs e)
        {
            if (ckShowFirstDerivative.IsChecked != null) _useFirstDerivative = (bool) ckShowFirstDerivative.IsChecked;
            ckDisplayRaw.IsChecked = false;
            ckDisplayRaw.Visibility = _useFirstDerivative ? Visibility.Collapsed : Visibility.Visible;
            RefreshCultureGraphImageSources(false);
        }

        private void CkDisplayQIdx_OnClick(object sender, RoutedEventArgs e)
        {
            if (ckDisplayQIdx.IsChecked != null) _displayQIndexes = (bool) ckDisplayQIdx.IsChecked;
            UpdateListView();
        }

        private void ckDisplayRaw_Click(object sender, RoutedEventArgs e)
        {
            RefreshCultureGraphImageSources(ckDisplayRaw.IsChecked != null && (bool)ckDisplayRaw.IsChecked);
        }

        private void RefreshCultureGraphImageSources(bool displayRaw)
        {
            var experimentalRun = (ExperimentalRun) lstViewExperiments.DataContext;
            foreach (var culture in experimentalRun.Run)
            {
                UpdateProgressText("Recalculating: " + culture.Container);
                DoEvents();
                var validTypes = new List<DataType>();
                if (_useFirstDerivative)
                    validTypes.Add(DataType.FirstDerivative);
                else
                {
                    validTypes.Add(DataType.Processed);
                    if ((bool)ckDisplayRaw.IsChecked)
                        validTypes.Add(DataType.Raw);
                }
                culture.GraphImageSource = ChartHelper.CreateChartImage(experimentalRun, culture.ContainerIndex, validTypes, 96, 96, 0, true);
            }

            UpdateProgressText("Updating View..");
            DoEvents();
            foreach (var item in lstViewExperiments.Items)
            {
                var i = (ListViewItem) lstViewExperiments.ItemContainerGenerator.ContainerFromItem(item);

                var contentPresenter = FindVisualChild<ContentPresenter>(i);

                var img = (Image) i.ContentTemplate.FindName("GraphImage", contentPresenter);
                var bindingExp = img.GetBindingExpression(Image.SourceProperty);
                bindingExp.UpdateTarget();
            }
            UpdateProgressText("");
        }

        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }

            return null;
        }

        private void MergeCultureSelection()
        {
            if (lstViewExperiments.SelectedItems.Count <= 1)
                return;

            UpdateProgressText("Merging Selection ..");

            var wellList = new List<int>();
            var experimentalRun = (ExperimentalRun)lstViewExperiments.DataContext;
            foreach (var item in lstViewExperiments.SelectedItems)
            {
                var culture = (Culture) item;
                wellList.Add( experimentalRun.Run.IndexOf(culture));
                UpdateProgressText("Adding culture:" + culture.Container);
            }
            UpdateProgressText("Done creating...");
            UpdateProgressText("Creating new Tab...");
            CreateZoomTab(wellList, experimentalRun);
            UpdateProgressText("");
        }

        private static void DoEvents()
        {
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new EmptyDelegate(delegate { }));
        }

        private void cntxMnuOrderSort_Click(object sender, RoutedEventArgs e)
        {
            var menuitem = (MenuItem) sender;
            _lstViewDirection = (string) menuitem.Tag == "ASC" ? ListSortDirection.Ascending : ListSortDirection.Descending;

            CommandBindings[0].Command.Execute(lstViewExperiments.Items.SortDescriptions.Last().PropertyName);
        }

        private void SortListViewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            UpdateProgressText("Sorting View ... Please Wait");
            SetIndeterminateProgressBar(true);
            
            var field = (string)e.Parameter;

            if (field == "ContainerName")
                field = "ContainerIndex";

            _field = field;
            Mouse.OverrideCursor = Cursors.Wait;

            NoArgDelegate fetcher = new NoArgDelegate(this.SortView2);

            fetcher.BeginInvoke(null, null);
        }

        private delegate void NoArgDelegate();
        private delegate void OneArgDelegate(String arg);
        
        private string _field;
        public void SortView2()
        {
            //Thread.Sleep(3000);
            
            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate()
            {
            lstViewExperiments.Items.SortDescriptions.Clear();
            lstViewExperiments.Items.SortDescriptions.Add(new SortDescription(_field, _lstViewDirection));
            });

            lstViewExperiments.Dispatcher.Invoke(
                DispatcherPriority.Normal,
                new OneArgDelegate(UpdateUserInterface),
                string.Empty);

        }

        private void UpdateUserInterface(String msg)
        {
            UpdateProgressText("");
            Mouse.OverrideCursor = null;
            SetIndeterminateProgressBar(false);
        }

        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (lstView.SelectedIndex == 0)
            {
                var selectedExperimentalRun = (ExperimentalRun)lstViewExperiments.DataContext;
                var paste = selectedExperimentalRun.GetTabularDelimitedOutput();
                Clipboard.SetText(paste);
                return;
            }
            var item = (ListViewItem)e.Parameter;
            var cell = GetVisualChild<ContentPresenter>(item);
            var rtb = new RenderTargetBitmap((int)item.ActualWidth, (int)item.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(cell);
            var resultBitmap2 = AddWhiteBacround(rtb);
            Clipboard.SetImage(resultBitmap2);
        }

        private static RenderTargetBitmap AddWhiteBacround(RenderTargetBitmap renderBitmap)
        {
            // Create a white background render bitmap
            var dWidth = (int) renderBitmap.Width;
            var dHeight = (int) renderBitmap.Height;
            var bg = WhiteBackground(dWidth, dHeight);

            // Adding those two render bitmap to the same drawing visual
            DrawingVisual dv = new DrawingVisual();
            DrawingContext dc = dv.RenderOpen();
            dc.DrawImage(bg, new Rect(new Size(dWidth, dHeight)));
            dc.DrawImage(renderBitmap, new Rect(new Size(dWidth, dHeight)));
            dc.Close();

            // Render the result
            var resultBitmap = new RenderTargetBitmap((int) renderBitmap.Width, (int) renderBitmap.Height, 96d, 96d, PixelFormats.Pbgra32);
            resultBitmap.Render(dv);
            return resultBitmap;
        }

        private static BitmapSource WhiteBackground(int dWidth, int dHeight)
        {
            int dStride = dWidth*4;
            byte[] pixels = new byte[dHeight*dStride];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = 0xFF;
            BitmapSource bg = BitmapSource.Create(dWidth, dHeight, 96, 96, PixelFormats.Pbgra32, null, pixels, dStride);
            return bg;
        }

        public T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        private void Copy_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            //e.CanExecute = lstView.SelectedIndex == 0;
        }

        private void MergeSelection_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MergeCultureSelection();
        }

        private void MergeSelection_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = lstViewExperiments.SelectedItems.Count > 1;
        }

        private void MarkAsExcluded_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (var selectedItem in lstViewExperiments.SelectedItems)
            {
                var culture = (Culture)selectedItem;
                culture.IsFaulty = culture.IsFaulty != true;
            }
            lstViewExperiments.Items.Refresh();
        }

        private void MarkAsExcluded_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _experimentViewType != ExperimentType.Samples;
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            var selectedExpander = (Expander) sender;
            foreach (var child in ActionsPanel.Children)
            {
                if (child.GetType() != typeof (Expander))
                    continue;
                var expander = (Expander) child;
                if (expander.Name != selectedExpander.Name)
                    expander.IsExpanded = false;
            }


        }

        
    }
}
