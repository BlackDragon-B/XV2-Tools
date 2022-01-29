﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using Xv2CoreLib.HslColor;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using LB_Common.Numbers;
using YAXLib;

namespace Xv2CoreLib.EMM
{
    [Serializable]
    [YAXSerializeAs("EMM")]
    public class EMM_File : INotifyPropertyChanged
    {
        #region NotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("Version")]
        public int Version { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Material")]
        public AsyncObservableCollection<EmmMaterial> Materials { get; set; } = new AsyncObservableCollection<EmmMaterial>();
        [YAXSerializeAs("UnknownData")]
        [YAXDontSerializeIfNull]
        public UnknownData Unknown_Data { get; set; }

        #region UI Stuff
        private EmmMaterial _selectedMaterial = null;
        [YAXDontSerialize]
        public EmmMaterial SelectedMaterial
        {
            get
            {
                return this._selectedMaterial;
            }
            set
            {
                if (value != this._selectedMaterial)
                {
                    this._selectedMaterial = value;
                    NotifyPropertyChanged("SelectedMaterial");
                }
            }
        }

        //Search
        private string _materialSearchFilter = null;
        [YAXDontSerialize]
        public string MaterialSearchFilter
        {
            get
            {
                return this._materialSearchFilter;
            }

            set
            {
                if (value != this._materialSearchFilter)
                {
                    this._materialSearchFilter = value;
                    NotifyPropertyChanged("MaterialSearchFilter");
                }
            }
        }

        [NonSerialized]
        private ListCollectionView _viewMaterials = null;
        [YAXDontSerialize]
        public ListCollectionView ViewMaterials
        {
            get
            {
                if (_viewMaterials != null)
                {
                    return _viewMaterials;
                }
                _viewMaterials = new ListCollectionView(Materials.Binding);
                _viewMaterials.Filter = new Predicate<object>(MaterialFilterCheck);
                return _viewMaterials;
            }
            set
            {
                if (value != _viewMaterials)
                {
                    _viewMaterials = value;
                    NotifyPropertyChanged("ViewMaterials");
                }
            }
        }

        public bool MaterialFilterCheck(object matObject)
        {
            if (String.IsNullOrWhiteSpace(MaterialSearchFilter)) return true;
            var _material = matObject as EmmMaterial;

            if (_material != null)
            {
                if (_material.Name.ToLower().Contains(MaterialSearchFilter.ToLower())) return true;
            }

            return false;
        }

        public void UpdateMaterialFilter()
        {
            if (_viewMaterials == null)
                _viewMaterials = new ListCollectionView(Materials.Binding);

            _viewMaterials.Filter = new Predicate<object>(MaterialFilterCheck);
            NotifyPropertyChanged("ViewMaterials");
        }

        #endregion

        #region LoadSave
        public static EMM_File LoadEmm(byte[] bytes)
        {
            return new Parser(bytes).emmFile;
        }

        /// <summary>
        /// Loads the specified emm file. It can be in either binary or xml format. 
        /// 
        /// If a file can not be found at the specified location, then a empty one will be returned.
        /// </summary>
        public static EMM_File LoadEmm(string path, bool returnEmptyIfNotValid = true)
        {
            if (Path.GetExtension(path) == ".emm")
            {
                return new Parser(path, false).GetEmmFile();
            }
            else if (Path.GetExtension(path) == ".xml" && Path.GetExtension(Path.GetFileNameWithoutExtension(path)) == ".emm")
            {
                YAXSerializer serializer = new YAXSerializer(typeof(EMM_File), YAXSerializationOptions.DontSerializeNullObjects);
                return (EMM_File)serializer.DeserializeFromFile(path);
            }
            else
            {
                if (returnEmptyIfNotValid)
                {
                    return new EMM_File()
                    {
                        Version = 0,
                        Materials = AsyncObservableCollection<EmmMaterial>.Create()
                    };
                }
                else
                {
                    throw new FileNotFoundException("An .emm could not be found at the specified location.");
                }

            }
        }

        public void SaveXmlEmmFile(string saveLocation)
        {
            if (!Directory.Exists(Path.GetDirectoryName(saveLocation)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveLocation));
            }
            YAXSerializer serializer = new YAXSerializer(typeof(EMM_File));
            serializer.SerializeToFile(this, saveLocation);
        }

        public void SaveBinaryEmmFile(string saveLocation)
        {
            if (!Directory.Exists(Path.GetDirectoryName(saveLocation)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveLocation));
            }
            new Deserializer(saveLocation, this);
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        #endregion

        #region Get/Helpers
        public EmmMaterial GetMaterial(string name)
        {
            foreach (var e in Materials)
            {
                if (e.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return e;
                }
            }

            return null;
        }

        public EmmMaterial GetEntry(int index)
        {
            if (index >= Materials.Count || index < 0)
            {
                throw new InvalidDataException(String.Format("EMM_File.GetEntry (int index): index out of range.\nindex = {0}", index));
            }

            return Materials[index];
        }

        public string GetUnusedName(string name)
        {
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
            string extension = Path.GetExtension(name);
            string newName = name;
            int num = 1;

            while (NameUsed(newName))
            {
                newName = String.Format("{0}_{1}{2}", nameWithoutExtension, num, extension);
                num++;
            }

            return newName;
        }

        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors = new List<RgbColor>();
            if (Materials == null) return colors;

            foreach (var mat in Materials)
            {
                colors.AddRange(mat.GetUsedColors());
            }

            return colors;
        }

        //Name Helpers:
        public bool NameUsed(string name)
        {
            foreach (var entry in Materials)
            {
                if (entry.Name == name) return true;

            }

            return false;
        }

        public void ValidateNames()
        {
            List<string> names = new List<string>();

            for (int i = 0; i < Materials.Count; i++)
            {
                if (names.Contains(Materials[i].Name))
                {
                    //Name was used previously
                    Materials[i].Name = GetUnusedName(Materials[i].Name);
                }
                else
                {
                    //Name is unused
                    names.Add(Materials[i].Name);
                }
            }
        }

        #endregion

        public static EMM_File DefaultEmmFile()
        {
            return new EMM_File()
            {
                Version = 16,
                Materials = AsyncObservableCollection<EmmMaterial>.Create(),
                Unknown_Data = new UnknownData()
            };
        }

        public EmmMaterial Compare(EmmMaterial emmEntry2, bool compareName = false)
        {
            foreach (var entry in Materials)
            {
                if (entry.Compare(emmEntry2, compareName))
                {
                    return entry;
                }
            }

            return null;
        }

        public void ChangeHsl(double hue, double saturation, double lightness, List<IUndoRedo> undos = null, bool hueSet = false, int variance = 0)
        {
            if (Materials == null) return;
            if (undos == null) undos = new List<IUndoRedo>();

            foreach (var mat in Materials)
            {
                mat.ChangeHsl(hue, saturation, lightness, undos, hueSet, variance);
            }
        }
    
        public void DecompileMaterials()
        {
            foreach (var mat in Materials)
                mat.DecompileParameters();
        }

        public void CompileMaterials()
        {
            foreach (var mat in Materials)
                mat.CompileParameters();
        }
    }

    [Serializable]
    [YAXSerializeAs("Material")]
    public class EmmMaterial
    {
        public static readonly string[] CommonShaderPrograms = new string[]
        {
            "TOON_UNIF_STAIN1_DFD",
            "TOON_UNIF_STAIN2_DFD",
            "TOON_UNIF_STAIN3_DFD",
            "TOON_UNIF_STAIN1_DFDAth",
            "TOON_UNIF_STAIN2_DFDAth",
            "TOON_UNIF_STAIN3_DFDAth",
            "TOON_STAIN1_DFD",
            "TOON_STAIN2_DFD",
            "TOON_DFD",
            "TOON_UNIF_DFD",

            "TOON_UNIF_EYE_MUT1_DFD",
            "TOON_UNIF_EYE_MUT2_DFD",
            "TOON_UNIF_EYE_MUT3_DFD",

            "ParticleDecal",
            "ParticleDecalPT",
            "SoftParticleDecal",
            "ParticleRefract",
            "DecalColor",

            "T1_VFX",
            "T1_VFX_MTN",
            "T1_VFX_MTN_DIS_ALPHA",
            "T1_VFX_MTN_REFRACT",
            "T1_SM_DYN_MTN",
        };

        [YAXAttributeForClass]
        public int Index { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public string Name { get; set; } //max 32
        [YAXAttributeForClass]
        [YAXSerializeAs("Shader")]
        public string ShaderProgram { get; set; } //max 32
        [YAXAttributeFor("I_66")]
        [YAXSerializeAs("value")]
        public ushort I_66 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Parameter")]
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();

        [YAXDontSerialize]
        public DecompiledMaterial DecompiledParameters { get; set; }

        public void DecompileParameters()
        {
            DecompiledParameters = DecompiledMaterial.Decompile(this);
        }

        public void CompileParameters()
        {
            Parameters = DecompiledParameters.Compile();
        }

        public bool Compare(EmmMaterial material2, bool compareName = false)
        {
            if (Name != material2.Name && compareName)
                return false;

            if (ShaderProgram != material2.ShaderProgram)
                return false;

            if (I_66 != material2.I_66)
                return false;

            if (!DecompiledParameters.Compare(material2.DecompiledParameters))
                return false;

            /*
              Parameters are now in DecompiledParameters!

            if (material2.Parameters.Count != Parameters.Count)
                return false;

            for(int i = 0; i < Parameters.Count; i++)
            {
                if (!Parameters[i].Compare(material2.Parameters[i]))
                    return false;
            }
            */

            return true;
        }

        public static EmmMaterial NewMaterial()
        {
            return new EmmMaterial()
            {
                Name = "NewMaterial",
                ShaderProgram = "ParticleDecal",
                DecompiledParameters = DecompiledMaterial.Default()
            };
        }

        public void ChangeHsl(double hue, double saturation = 0.0, double lightness = 0.0, List<IUndoRedo> undos = null, bool hueSet = false, int variance = 0)
        {
            if (Parameters == null) return;

            foreach (var param in DecompiledMaterial.ColorParameters)
            {
                CustomColor col = DecompiledParameters.GetColor(param);

                if (col.R != 0 || col.G != 0 || col.B != 0)
                {
                    //Create rgbColor
                    RgbColor rgbColor = new RgbColor(col.R, col.G, col.B);
                    var hslColor = rgbColor.ToHsl();
                    RgbColor newColor;

                    if (hueSet)
                    {
                        hslColor.SetHue(hue, variance);
                    }
                    else
                    {
                        hslColor.ChangeHue(hue);
                        hslColor.ChangeLightness(lightness);
                        hslColor.ChangeSaturation(saturation);
                    }

                    newColor = hslColor.ToRgb();
                    float R = (float)newColor.R;
                    float G = (float)newColor.G;
                    float B = (float)newColor.B;

                    undos.Add(new UndoablePropertyGeneric(nameof(col.R), col, col.R, R));
                    undos.Add(new UndoablePropertyGeneric(nameof(col.G), col, col.G, G));
                    undos.Add(new UndoablePropertyGeneric(nameof(col.B), col, col.B, B));

                    //Add new color to parameters
                    col.R = R;
                    col.G = G;
                    col.B = B;
                }
            }
        }

        #region Get
        internal string GetValue(string parameter, bool returnDefault = false)
        {
            foreach (var e in Parameters)
            {
                if (e.Name == parameter)
                {
                    return e.Value;
                }
            }

            return returnDefault ? "0" : null;
        }

        internal Parameter GetParameter(string parameterName)
        {
            if (Parameters == null) Parameters = new List<Parameter>();

            foreach (var parameter in Parameters)
            {
                if (parameter.Name == parameterName) return parameter;
            }

            return null;
        }

        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors = new List<RgbColor>();
            if (Parameters == null) return colors;

            foreach (var param in DecompiledMaterial.ColorParameters)
            {
                CustomColor col = DecompiledParameters.GetColor(param);

                if (col.R != 0 || col.G != 0 || col.B != 0)
                {
                    var color = new RgbColor(col.R, col.G, col.B);

                    if (!color.IsWhiteOrBlack)
                    {
                        colors.Add(color);
                    }
                }
            }

            return colors;
        }

        #endregion
    }

    [Serializable]
    [YAXSerializeAs("Parameter")]
    public class Parameter
    {
        public enum ParameterType : int
        {
            Float = 0,
            Int = 65537,
            Float2 = 65536,
            Bool = 1
        }

        //Properties
        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public string Name { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public ParameterType Type { get; set; } 
        [YAXAttributeForClass]
        [YAXSerializeAs("value")]
        public string Value { get; set; }

        //Value Properties
        [YAXDontSerialize]
        public float FloatValue
        {
            get
            {
                float _float;
                if(float.TryParse(Value, out _float))
                {
                    return _float;
                }
                return 0f;
            }
        }
        [YAXDontSerialize]
        public int IntValue
        {
            get
            {
                if (Value.ToLower() == "true") return 1;
                if (Value.ToLower() == "false") return 0;

                int _intValue;
                if (int.TryParse(Value, out _intValue))
                {
                    return _intValue;
                }
                return 0;
            }
        }

        public Parameter() { }

        public Parameter(string name, ParameterType type, string value)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        public bool Compare(Parameter parameter2)
        {
            if (Name != parameter2.Name)
                return false;
            
            if (Type != parameter2.Type)
                return false;

            if (Value != parameter2.Value)
                return false;

            return true;
        }

    }

    [Serializable]
    public class UnknownData
    {
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public int I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public int I_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        public int I_64 { get; set; }
    }

}
