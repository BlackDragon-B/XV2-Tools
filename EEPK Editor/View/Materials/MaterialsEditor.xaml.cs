﻿using System;
using System.Linq;
using System.Windows;
using System.Text;
using System.Windows.Data;
using System.Windows.Controls;
using System.ComponentModel;
using System.Collections.Generic;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.UndoRedo;
using EEPK_Organiser.Forms;
using EEPK_Organiser.Forms.Recolor;
using MahApps.Metro.Controls.Dialogs;
using GalaSoft.MvvmLight.CommandWpf;
using EEPK_Organiser.ViewModel;

namespace EEPK_Organiser.View
{
    /// <summary>
    /// Interaction logic for MaterialsEditor.xaml
    /// </summary>
    public partial class MaterialsEditor : UserControl, INotifyPropertyChanged, IDisposable
    {
        #region NotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        
        #region DependencyProperty
        public static readonly DependencyProperty EmmFileProperty = DependencyProperty.Register(nameof(EmmFile), typeof(EMM_File), typeof(MaterialsEditor), new PropertyMetadata(null));

        public EMM_File EmmFile
        {
            get { return (EMM_File)GetValue(EmmFileProperty); }
            set 
            { 
                SetValue(EmmFileProperty, value); 
                NotifyPropertyChanged(nameof(EmmFile));
            }
        }

        public static readonly DependencyProperty AssetContainerProperty = DependencyProperty.Register(nameof(AssetContainer), typeof(AssetContainerTool), typeof(MaterialsEditor), new PropertyMetadata(null));

        public AssetContainerTool AssetContainer
        {
            get { return (AssetContainerTool)GetValue(AssetContainerProperty); }
            set
            { 
                SetValue(AssetContainerProperty, value);
                NotifyPropertyChanged(nameof(AssetContainer));
                NotifyPropertyChanged(nameof(ContainerVisiblility));
                NotifyPropertyChanged(nameof(IsForContainer));
            }
        }

        #endregion

        //UI
        private EmmMaterial _selectedMaterial = null;
        public EmmMaterial SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                _selectedMaterial = value;
                MaterialViewModel = (_selectedMaterial != null) ? new MaterialViewModel(_selectedMaterial.DecompiledParameters) : null;

                NotifyPropertyChanged(nameof(SelectedMaterial));
                NotifyPropertyChanged(nameof(MaterialViewModel));
                NotifyPropertyChanged(nameof(ParameterEditorEnabled));
            }
        }
        public bool ParameterEditorEnabled => _selectedMaterial != null;

        //ViewModel
        private MaterialViewModel mat = null;
        public MaterialViewModel MaterialViewModel
        {
            set => mat = value;
            get
            {
                return mat;
            }
        }

        //Selected Material Editing
        public string SelectedMaterialName
        {
            get => _selectedMaterial?.Name;
            set
            {
                if(value != _selectedMaterial.Name)
                {
                    SetName(value);
                }
            }
        }
        public string SelectedMaterialShaderProgram
        {
            get => _selectedMaterial.ShaderProgram;
            set
            {
                if(value != _selectedMaterial.ShaderProgram)
                {
                    SetShaderProgram(value);
                }
            }
        }

        //Properties for disabling and hiding elements that aren't needed in the current context. (e.g: dont show merge/used by options when editing a emm file for a emo or character as those are only useful for PBIND/TBIND assets)
        public bool IsForContainer => AssetContainer != null;
        public Visibility ContainerVisiblility => IsForContainer ? Visibility.Visible : Visibility.Collapsed;

        #region ViewMaterials
        private ListCollectionView _viewMaterials = null;
        public ListCollectionView ViewMaterials
        {
            get
            {
                if (_viewMaterials == null && EmmFile != null)
                {
                    _viewMaterials = new ListCollectionView(EmmFile.Materials.Binding);
                    _viewMaterials.Filter = new Predicate<object>(SearchFilterCheck);
                }
                return _viewMaterials;
            }
            set
            {
                if (value != _viewMaterials)
                {
                    _viewMaterials = value;
                    NotifyPropertyChanged(nameof(ViewMaterials));
                }
            }
        }

        private string _searchFilter = null;
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                _searchFilter = value;
                RefreshViewMaterials();
                NotifyPropertyChanged(nameof(SearchFilter));
            }
        }

        private void RefreshViewMaterials()
        {
            if(_viewMaterials == null)
                _viewMaterials = new ListCollectionView(EmmFile.Materials.Binding);

            _viewMaterials.Filter = new Predicate<object>(SearchFilterCheck);
            NotifyPropertyChanged(nameof(ViewMaterials));
        }

        public bool SearchFilterCheck(object material)
        {
            if (string.IsNullOrWhiteSpace(SearchFilter)) return true;
            var _material = material as EmmMaterial;
            string flattenedSearchParam = SearchFilter.ToLower();

            if (_material != null)
            {
                //Search is for material name or shader program
                if (_material.Name.ToLower().Contains(flattenedSearchParam) || _material.ShaderProgram.ToLower().Contains(flattenedSearchParam))
                {
                    return true;
                }

                //Search for parameters that are "used" (dont have default values)
                if (_material.DecompiledParameters.HasParameter(SearchFilter))
                    return true;
            }

            return false;
        }


        //Filtering
        public RelayCommand ClearSearchCommand => new RelayCommand(ClearSearch);
        private void ClearSearch()
        {
            SearchFilter = string.Empty;
        }
        #endregion

        public MaterialsEditor()
        {
            DataContext = this;
            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            Loaded += MaterialsEditor_Loaded;

        }

        private void MaterialsEditor_Loaded(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(ContainerVisiblility));
            RefreshViewMaterials();
        }

        private void Instance_UndoOrRedoCalled(object sender, EventArgs e)
        {
            UpdateProperties();
        }

        private void UpdateProperties()
        {
            NotifyPropertyChanged(nameof(SelectedMaterialName));
            NotifyPropertyChanged(nameof(SelectedMaterialShaderProgram));

            //If properties on the material itself have changed externally (such as on a undo/redo), then the list needs to be refreshed.
            materialDataGrid.Items.Refresh();
        }

        public void Dispose()
        {
            Loaded -= MaterialsEditor_Loaded;
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        private async void SetName(string name)
        {
            //Name should never be more than 32 since the UI is limited to just 32 characters, but just in case we will trim the name here if it exceeds the limit.
            if (name.Length > 32)
                name = name.Substring(0, 32);
            
            if (EmmFile.Materials.Any(x => x.Name == name && x != _selectedMaterial))
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Name Already Used", $"Another material is already named\"{name}\".", MessageDialogStyle.Affirmative, DialogSettings.Default);
                NotifyPropertyChanged(nameof(SelectedMaterialName));
                return;
            }

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(_selectedMaterial.Name), _selectedMaterial, _selectedMaterial.Name, name, "Material Name"));
            _selectedMaterial.Name = name;
            NotifyPropertyChanged(nameof(SelectedMaterialName));
        }

        private void SetShaderProgram(string shader)
        {
            //ShaderProgram should never be more than 32 since the UI is limited to just 32 characters, but just in case we will trim the name here if it exceeds the limit.
            if (shader.Length > 32)
                shader = shader.Substring(0, 32);

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(_selectedMaterial.ShaderProgram), _selectedMaterial, _selectedMaterial.ShaderProgram, shader, "Material ShaderProgram"));
            _selectedMaterial.ShaderProgram = shader;
            NotifyPropertyChanged(nameof(SelectedMaterialShaderProgram));
        }

        #region ContextMenuCommands
        public RelayCommand AddNewMaterialCommand => new RelayCommand(AddNewMaterial);
        private void AddNewMaterial()
        {
            EmmMaterial material = EmmMaterial.NewMaterial();
            material.Name = EmmFile.GetUnusedName(material.Name);

            EmmFile.Materials.Add(material);
            SelectedMaterial = material;
            materialDataGrid.ScrollIntoView(material);

            UndoManager.Instance.AddUndo(new UndoableListAdd<EmmMaterial>(EmmFile.Materials, material, "New Material"));
        }

        public RelayCommand DeleteMaterialCommand => new RelayCommand(DeleteMaterial, IsMaterialSelected);
        private async void DeleteMaterial()
        {
            bool materialInUse = false;
            int removed = 0;
            List<EmmMaterial> selectedMaterials = materialDataGrid.SelectedItems.Cast<EmmMaterial>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (selectedMaterials.Count > 0)
            {
                if (IsForContainer)
                {
                    foreach (var material in selectedMaterials)
                    {
                        if (AssetContainer.IsMaterialUsed(material))
                        {
                            materialInUse = true;
                        }
                        else
                        {
                            removed++;
                            AssetContainer.DeleteMaterial(material, undos);
                        }
                    }

                    if (materialInUse && selectedMaterials.Count == 1)
                    {
                        await DialogCoordinator.Instance.ShowMessageAsync(this, "Delete", "The selected material cannot be deleted because it is currently being used.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    }
                    else if (materialInUse && selectedMaterials.Count > 1)
                    {
                        await DialogCoordinator.Instance.ShowMessageAsync(this, "Delete", "One or more of the selected materials cannot be deleted because they are currently being used.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    }
                }
                else
                {
                    foreach (var material in selectedMaterials)
                    {
                        removed++;
                        EmmFile.Materials.Remove(material);
                        undos.Add(new UndoableListRemove<EmmMaterial>(EmmFile.Materials, material));
                    }
                }

                if (removed > 0)
                {
                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Delete Material"));
                }
            }
        }

        public RelayCommand DuplicateMaterialCommand => new RelayCommand(DuplicateMaterial, IsMaterialSelected);
        private void DuplicateMaterial()
        {
            List<EmmMaterial> selectedMaterials = materialDataGrid.SelectedItems.Cast<EmmMaterial>().ToList();
            List<IUndoRedo> undos = new List<IUndoRedo>();


            foreach (var mat in selectedMaterials)
            {
                EmmMaterial newMaterial = mat.Copy();
                newMaterial.Name = EmmFile.GetUnusedName(newMaterial.Name);
                undos.Add(new UndoableListAdd<EmmMaterial>(EmmFile.Materials, newMaterial));
                EmmFile.Materials.Add(newMaterial);

                SelectedMaterial = newMaterial;
            }

            materialDataGrid.ScrollIntoView(SelectedMaterial);

            if (undos.Count > 0)
                UndoManager.Instance.AddCompositeUndo(undos, "Duplicate Material(s)");
        }

        public RelayCommand MergeMaterialCommand => new RelayCommand(MergeMaterial, IsMaterialSelected);
        private async void MergeMaterial()
        {
            List<EmmMaterial> selectedMaterials = materialDataGrid.SelectedItems.Cast<EmmMaterial>().ToList();
            selectedMaterials.Remove(SelectedMaterial);

            if (SelectedMaterial != null && selectedMaterials.Count > 0)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int count = selectedMaterials.Count + 1;

                var result = await DialogCoordinator.Instance.ShowMessageAsync(this, string.Format("Merge ({0} materials)", count), string.Format("All currently selected materials will be MERGED into {0}.\n\nAll other selected materials will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", SelectedMaterial.Name), MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

                if (result == MessageDialogResult.Affirmative)
                {
                    foreach (var materialToRemove in selectedMaterials)
                    {
                        AssetContainer.RefactorMaterialRef(materialToRemove, SelectedMaterial, undos);
                        undos.Add(new UndoableListRemove<EmmMaterial>(AssetContainer.File2_Ref.Materials, materialToRemove));
                        AssetContainer.File2_Ref.Materials.Remove(materialToRemove);
                    }

                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge (Material)"));
                }
            }
            else
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Merge", "Cannot merge with less than 2 materials selected.\n\nTip: Use Left Ctrl + Left Mouse Click to multi-select.", MessageDialogStyle.Affirmative, DialogSettings.Default);
            }
        }

        public RelayCommand UsedByCommand => new RelayCommand(UsedBy, IsMaterialSelected);
        private void UsedBy()
        {
            if (SelectedMaterial != null)
            {
                List<string> assets = AssetContainer.MaterialUsedBy(SelectedMaterial);
                assets.Sort();
                StringBuilder str = new StringBuilder();

                foreach (var asset in assets)
                {
                    str.Append(String.Format("{0}\r", asset));
                }

                LogForm logForm = new LogForm("The following assets use this material", str.ToString(), string.Format("{0}: Used By", SelectedMaterial.Name), null, true);
                logForm.Show();
            }
        }

        public RelayCommand HueShiftCommand => new RelayCommand(HueShift, IsMaterialSelected);
        private void HueShift()
        {
            if (SelectedMaterial != null)
            {
                RecolorAll recolor = new RecolorAll(SelectedMaterial, Application.Current.MainWindow);

                if (recolor.Initialize())
                    recolor.ShowDialog();
            }
        }

        public RelayCommand HueSetCommand => new RelayCommand(HueSet, IsMaterialSelected);
        private void HueSet()
        {
            if (SelectedMaterial != null)
            {
                RecolorAll_HueSet recolor = new RecolorAll_HueSet(SelectedMaterial, Application.Current.MainWindow);

                if (recolor.Initialize())
                    recolor.ShowDialog();
            }
        }

        public RelayCommand CopyMaterialCommand => new RelayCommand(CopyMaterial, IsMaterialSelected);
        private void CopyMaterial()
        {
            List<EmmMaterial> selectedMaterials = materialDataGrid.SelectedItems.Cast<EmmMaterial>().ToList();

            if(selectedMaterials != null)
            {
                Clipboard.SetData(Misc.ClipboardDataTypes.EmmMaterial, selectedMaterials);
            }
        }

        public RelayCommand PasteMaterialCommand => new RelayCommand(PasteMaterial, CanPasteMaterial);
        private void PasteMaterial()
        {
            List<EmmMaterial> copiedMaterials = (List<EmmMaterial>)Clipboard.GetData(Misc.ClipboardDataTypes.EmmMaterial);

            if(copiedMaterials != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach(var material in copiedMaterials)
                {
                    material.Name = EmmFile.GetUnusedName(material.Name);
                    EmmFile.Materials.Add(material);

                    undos.Add(new UndoableListAdd<EmmMaterial>(EmmFile.Materials, material));
                }

                UndoManager.Instance.AddCompositeUndo(undos, copiedMaterials.Count > 1 ? "Paste Materials" : "Paste Material");
            }

        }

        public RelayCommand PasteMaterialValuesCommand => new RelayCommand(PasteMaterialValues, CanPasteMaterialValues);
        private async void PasteMaterialValues()
        {
            List<EmmMaterial> copiedMaterials = (List<EmmMaterial>)Clipboard.GetData(Misc.ClipboardDataTypes.EmmMaterial);

            if (copiedMaterials != null)
            {
                if(copiedMaterials.Count == 0 || copiedMaterials.Count > 1)
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Paste Values", "Cannot paste the material values as there were more than 1 copied.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    return;
                }

                List<IUndoRedo> undos = SelectedMaterial.DecompiledParameters.PasteValues(copiedMaterials[0]);
                undos.Add(new UndoablePropertyGeneric(nameof(SelectedMaterial.ShaderProgram), SelectedMaterial, SelectedMaterial.ShaderProgram, copiedMaterials[0].ShaderProgram));
                SelectedMaterial.ShaderProgram = copiedMaterials[0].ShaderProgram;

                UndoManager.Instance.AddCompositeUndo(undos, "Paste Values");

                NotifyPropertyChanged(nameof(SelectedMaterialShaderProgram));
            }

        }



        private bool IsMaterialSelected()
        {
            return _selectedMaterial != null;
        }

        private bool CanPasteMaterial()
        {
            return Clipboard.ContainsData(Misc.ClipboardDataTypes.EmmMaterial);
        }

        private bool CanPasteMaterialValues()
        {
            return CanPasteMaterial() && IsMaterialSelected();
        }
        #endregion

        #region ToolsCommand
        public RelayCommand MergeDuplicatesCommand => new RelayCommand(MergeDuplicates);
        private async void MergeDuplicates()
        {
            var result = await DialogCoordinator.Instance.ShowMessageAsync(this, "Merge Duplicates", "All instances of duplicated materials will be merged into a single material. A duplicated material means any that share the same parameters, but have a different name. \n\nAll references to the duplicates in any assets will also be updated to reflect these changes.\n\nDo you want to continue?", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

            if(result == MessageDialogResult.Affirmative)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int duplicateCount = AssetContainer.MergeDuplicateMaterials(undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Merge Duplicates (Material)"));

                if (duplicateCount > 0)
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Merge Duplicates", string.Format("{0} material instances were merged.", duplicateCount), MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
                else
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Merge Duplicates", "No instances of duplicated materials were found.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
            }

        }

        public RelayCommand RemoveUnusedMaterialsCommand => new RelayCommand(RemoveUnusedMaterials);
        private async void RemoveUnusedMaterials()
        {
            var result = await DialogCoordinator.Instance.ShowMessageAsync(this, "Remove Unused", "Any material that is not currently used by a asset will be deleted.\n\nDo you want to continue?", MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

            if (result == MessageDialogResult.Affirmative)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                int duplicateCount = AssetContainer.RemoveUnusedMaterials(undos);

                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Remove Unused (Mats)"));

                if (duplicateCount > 0)
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Remove Unused ", string.Format("{0} material instances were removed.", duplicateCount), MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
                else
                {
                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Remove Unused ", "No materials were removed.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                }
            }

        }


        #endregion
    }
}
