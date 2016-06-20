using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Precog.Controls;

namespace Precog.DialogWindows
{
    /// <summary>
    /// Interaction logic for ReplicatePatternDialogBox.xaml
    /// </summary>
    public enum ReplicateNonMirror
    {
        Independent,
        Merged
    }
    
    public partial class ReplicatePatternDialogBox : Window
    {
        
        private bool _arePlateButtonsClickable = true;
        
        public bool MirrowPlates { get; set; }
        public ReplicateNonMirror NonMirrorBehaviour { get; set; }
        public int GapPlate1 { get; set; }
        public int GapPlate2 { get; set; }
        public ImageBrush SnapShot { get; set; }

        public ReplicatePatternDialogBox()
        {
            InitializeComponent();
            CreateWells();
            MirrowPlates = false;
            NonMirrorBehaviour = ReplicateNonMirror.Independent;
            SetExtendButtonsVisibility();
        }

        private void CreateWells()
        {
            int well = 0;
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    well += 1;
                    var btn = new ToggleButton
                                  {
                                      Name = "Plate1_well" + well.ToString(),
                                      Content = well.ToString(),
                                      Tag = well,
                                      Focusable = false
                                  };

                    btn.Click += new RoutedEventHandler(btnClick);
                    btn.PreviewMouseDown += new MouseButtonEventHandler(WellButton_PreviewMouseDown);
                    Grid.SetColumn(btn, i);
                    Grid.SetRow(btn, j);
                    if (well == 1)
                        btn.IsChecked = true;
                    Plate1.Children.Add(btn);
                    var btn2 = new ToggleButton
                                   {
                                       Name = "Plate2_well" + well.ToString(),
                                       Content = well.ToString(),
                                       Tag = well,
                                       Focusable = false
                                   };
                    btn2.Click += new RoutedEventHandler(btnClick);
                    btn2.PreviewMouseDown += new MouseButtonEventHandler(WellButton_PreviewMouseDown);
                    Grid.SetColumn(btn2, i);
                    Grid.SetRow(btn2, j);
                    Plate2.Children.Add(btn2);
                }
            }
        }

        private void MergePlates()
        {
            foreach (var child in Plate2.Children)
            {
                var btn = (ToggleButton)child;
                int well = int.Parse((string)btn.Content);
                btn.Content = (well + 100).ToString();
            }
        }

        private void BreakPlates()
        {
            foreach (var child in Plate2.Children)
            {
                var btn = (ToggleButton)child;
                btn.Content = btn.Tag.ToString();
            }
        }

        private void btnClick(object sender, RoutedEventArgs e)
        {
            var clickedButton = (ToggleButton) sender;
            MirrowButton(clickedButton);
        }

        private void MirrowButton(ToggleButton clickedButton)
        {
            if ((bool) ckMirrorPlates.IsChecked)
            {
                if (clickedButton.Name.Contains("Plate2"))
                {
                    clickedButton.IsChecked = !clickedButton.IsChecked;
                    return;
                }

                int index = Plate1.Children.IndexOf(clickedButton);
                var mirrowedButtun = (ToggleButton) Plate2.Children[index];
                mirrowedButtun.IsChecked = clickedButton.IsChecked;
                mirrowedButtun.Content = mirrowedButtun.Tag.ToString();
            }
        }

        private void ckMirrorPlates_Click(object sender, RoutedEventArgs e)
        {
            MirrowPlates = (bool) ckMirrorPlates.IsChecked;
            if (MirrowPlates)
            {
                MirrowPlate1();
                RbMirrowIndependate.IsChecked = true;
            }
            
            SetExtendButtonsVisibility();
        }

        private void SetExtendButtonsVisibility()
        {
            if (!MirrowPlates)
            {
                NonMirrorBehaviour = (bool) RbMirrowIndependate.IsChecked
                                          ? ReplicateNonMirror.Independent
                                          : ReplicateNonMirror.Merged;
            }
            if (MirrowPlates || NonMirrorBehaviour == ReplicateNonMirror.Merged)
            {
                ExtendPlate1.Visibility = Visibility.Collapsed;
                ExtendPlate2.Visibility = Visibility.Collapsed;
                ExtendAllPlates.Visibility = Visibility.Visible;
            }
            else
            {
                if ((bool) RbMirrowIndependate.IsChecked)
                {
                    ExtendAllPlates.Visibility = Visibility.Collapsed;
                    ExtendPlate1.Visibility = Visibility.Visible;
                    ExtendPlate2.Visibility = Visibility.Visible;
                }
            }
        }

        private void MirrowPlate1()
        {
            foreach (var child in Plate1.Children)
            {
                var btn = (ToggleButton) child;
                MirrowButton(btn);
            }
        }

        private void ExtrapolatePattern(Grid plate, int gap)
        {
            int fitGap = 0;
            for (int i = 0; i <= plate.Children.Count - 1; i++)
            {
                if (i == 0)
                    continue;

                fitGap += 1;
                var button = (ToggleButton)plate.Children[i];
                if (fitGap <= gap)
                    button.IsChecked = false;
                else
                {
                    button.IsChecked = true;
                    fitGap = 0;
                }

                if (plate.Name == "Plate1" && MirrowPlates)
                    MirrowButton(button);
            }
        }

        private void ExtrapolatePatternMerged(int gap)
        {
            int fitGap = 0;
            for (int i = 0; i <=  (Plate1.Children.Count + Plate2.Children.Count - 1); i++)
            {
                if (i == 0)
                    continue;

                fitGap += 1;
                ToggleButton button;
                if (i < 100)
                    button = (ToggleButton) Plate1.Children[i];
                else
                    button = (ToggleButton) Plate2.Children[i-100];

                if (fitGap <= gap)
                    button.IsChecked = false;
                else
                {
                    button.IsChecked = true;
                    fitGap = 0;
                }
            }
        }

        private int FindGap(Grid plate)
        {
            int gap = 0;
            bool foundFirstPattern = false;
            for (int i = 0; i <= plate.Children.Count - 1 && !foundFirstPattern; i++)
            {
                var button = (ToggleButton)plate.Children[i];
                if (button.IsChecked == true)
                {
                    if (i != 0)
                        foundFirstPattern = true;
                }
                else
                    gap += 1;
            }
            if (foundFirstPattern)
                return gap;
            
            return 0;
        }

        private int FindMergedPlatesGap()
        {
            int gap = 0;
            bool foundFirstPattern = false;
            for (int i = 0; i <= Plate1.Children.Count + Plate2.Children.Count - 1 && !foundFirstPattern; i++)
            {
                ToggleButton button;
                if (i < 100)
                    button = (ToggleButton)Plate1.Children[i];
                else
                    button = (ToggleButton)Plate2.Children[i - 100];

                if (button.IsChecked == true)
                {
                    if (i != 0)
                        foundFirstPattern = true;
                }
                else
                    gap += 1;
            }
            if (foundFirstPattern)
                return gap;

            return 0;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in Plate1.Children)
            {
                var button = (ToggleButton) child;
                button.IsChecked = false;
            }
            foreach (var child in Plate2.Children)
            {
                var button = (ToggleButton)child;
                button.IsChecked = false;
            }
            Validate.Content = "Validate Pattern";
            EnableOperations(true);
        }

        private void EnableOperations(bool enable)
        {
            EnablePlateButtons(enable);
            ExtendAllPlates.IsEnabled = enable;
            ckMirrorPlates.IsEnabled = enable;
            grpRadioButtons.IsEnabled = enable;
            ExtendPlate1.IsEnabled = enable;
            ExtendPlate2.IsEnabled = enable;
        }

        private bool ValidatePatterns()
        {
            if (!MirrowPlates && NonMirrorBehaviour == ReplicateNonMirror.Merged)
            {
                if(!ValidateMergedPattern())
                {
                    MessageBox.Show("Merged Plates Error: The current selection of wells has no valid pattern");
                    return false;
                }
            }
            else
            {
                if (!ValidatePattern(Plate1))
                {
                    MessageBox.Show("Plate1 Error: The current selection of wells has no valid pattern");
                    return false;
                }
                if (!ValidatePattern(Plate2))
                {
                    MessageBox.Show("Plate2 Error: The current selection of wells has no valid pattern");
                    return false;
                }
            }
            return true;
        }

        private bool ValidateMergedPattern()
        {
            int gap = FindMergedPlatesGap();

            if (gap == 0)
                return false;

            int fitGap = 0;
            for (int i = 0; i <= (Plate1.Children.Count + Plate2.Children.Count - 1); i++)
            {
                if (i == 0)
                    continue;

                fitGap += 1;
                ToggleButton button;
                if (i < 100)
                    button = (ToggleButton)Plate1.Children[i];
                else
                    button = (ToggleButton)Plate2.Children[i - 100];
                
                if (fitGap <= gap)
                {
                    if (button.IsChecked != false)
                        return false;
                }
                else
                {
                    if (button.IsChecked != true)
                        return false;
                    fitGap = 0;
                }
            }
            GapPlate1 = gap;
            return true;
        }

        private bool ValidatePattern(Grid plate)
        {
            int gap = FindGap(plate);

            if (gap == 0)
                return false;

            int fitGap = 0;
            for (int i = 0; i <= plate.Children.Count - 1; i++)
            {
                if (i == 0)
                    continue;

                fitGap += 1;
                var button = (ToggleButton)plate.Children[i];
                if (fitGap <= gap)
                {
                    if (button.IsChecked != false)
                        return false;
                }
                else
                {
                    if (button.IsChecked != true)
                        return false;
                    fitGap = 0;
                }
            }
            if (plate.Name == "Plate1")
                GapPlate1 = gap;
            else if (plate.Name == "Plate2")
                GapPlate2 = gap;
            return true;
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            if ((string)Validate.Content == "Accept")
            {
                this.DialogResult = true;
                var rtb = new RenderTargetBitmap((int)Plates.ActualWidth, (int)Plates.ActualHeight, 96, 96,PixelFormats.Pbgra32);
                rtb.Render(Plates);
                SnapShot = new ImageBrush(rtb);

            }
            else
            {
                bool patternValid = ValidatePatterns();
                if (patternValid)
                {
                    Validate.Content = "Accept";
                    EnableOperations(false);
                }
            }
        }

        private void EnablePlateButtons(bool enable)
        {
            _arePlateButtonsClickable = enable;
        }

        private void WellButton_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            e.Handled = !_arePlateButtonsClickable;
        }

        private void ExtendAllPlates_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)!((ToggleButton)Plate1.Children[0]).IsChecked)
            {
                MessageBox.Show("Plate 1 error: You need to start the pattern from position 1");
                return;
            }

            int gap = FindMergedPlatesGap();
            if (!MirrowPlates && NonMirrorBehaviour == ReplicateNonMirror.Merged)
                ExtrapolatePatternMerged(gap);
            else
                ExtrapolatePattern(Plate1, gap);
        }

        private void ExtendPlate1_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)!((ToggleButton)Plate1.Children[0]).IsChecked)
            {
                MessageBox.Show("Plate 1 error: You need to start the pattern from position 1");
                return;
            }

            int gap = FindGap(Plate1);
            ExtrapolatePattern(Plate1, gap);

        }

        private void ExtendPlate2_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)!((ToggleButton)Plate2.Children[0]).IsChecked)
            {
                MessageBox.Show("Plate 2 error: You need to start the pattern from position 1");
                return;
            }
            int gap = FindGap(Plate2);
            ExtrapolatePattern(Plate2, gap);

        }

        private void RbMirrowIndependate_Click(object sender, RoutedEventArgs e)
        {
            SetExtendButtonsVisibility();
            if ((bool)RbMirrowIndependate.IsChecked)
                BreakPlates();
        }

        private void RbMirrowMerged_Click(object sender, RoutedEventArgs e)
        {
            SetExtendButtonsVisibility();
            if ((bool) RbMirrowMerged.IsChecked)
                MergePlates();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            //RaiseEvent(new RoutedEventArgs(CancelButtonClickEvent));
            this.DialogResult = false;
        }
    }
}
