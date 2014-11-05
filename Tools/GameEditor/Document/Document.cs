﻿using System;
using Sitana.Framework.Cs;
using System.IO;
using Sitana.Framework.Ui.Binding;

namespace GameEditor
{
    public class Document: Singleton<Document>
    {
        public static Document Current
        {
            get
            {
                return Instance;
            }
        }

        int _nextIndex = 0;

        public readonly SharedString FileName = new SharedString();

        public string FilePath { get; private set;}

        public ItemsList<DocLayer> Layers { get; private set;}

        private int _layerIndex = 1;

        public bool IsModified { get; private set;}

        public event EmptyArgsVoidDelegate LayerSelectionChanged;

        public DocLayer SelectedLayer
        {
            get
            {
                for (int idx = 0; idx < Layers.Count; ++idx)
                {
                    if (Layers[idx].Selected.Value)
                    {
                        return Layers[idx];
                    }
                }

                return null;
            }
        }

        public void New()
        {
            _nextIndex++;
            FileName.Format("New {0}", _nextIndex);
            FilePath = null;

            Layers.Clear();

            foreach (var layer in CurrentTemplate.Instance.Layers)
            {
                DocLayer generated = layer.Generate();
                Layers.Add(generated);
                if (layer.Selected)
                {
                    Select(generated);
                }
            }

            _layerIndex = Layers.Count + 1;
            IsModified = false;
        }

        public Document()
        {
            Layers = new ItemsList<DocLayer>();
        }

        public void Save()
        {
            Save(FilePath);
        }

        public void Save(string path)
        {
            FilePath = path;
            FileName.StringValue = Path.GetFileNameWithoutExtension(path);

            IsModified = false;
        }

        public void CancelModified()
        {
            IsModified = false;
        }

        public void SetModified()
        {
            IsModified = true;
        }

        public void AddVectorLayer()
        {
            var layer = new DocVectorLayer(String.Format("LAYER {0}", _layerIndex));
            Layers.Add(layer);
            _layerIndex++;

            Select(layer);
            SetModified();
        }
            
        public void AddTilesetLayer()
        {
            var layer = new DocTiledLayer(String.Format("LAYER {0}", _layerIndex));
            layer.Layer.Tileset = CurrentTemplate.Instance.Tileset(null).Item1;
            Layers.Add(layer);
            _layerIndex++;

            Select(layer);
            SetModified();
        }



        public void RemoveSelectedLayer()
        {
            if ( Layers.Count == 1 )
            {
                return;
            }

            for( int idx = 0; idx < Layers.Count; ++idx )
            {
                if(Layers[idx].Selected.Value)
                {
                    Layers.RemoveAt(idx);
                    break;
                }
            }

            Select(Layers[0]);
            SetModified();
        }

        public void Select(DocLayer layer)
        {
            for( int idx = 0; idx < Layers.Count; ++idx )
            {
                Layers[idx].Selected.Value = false;
            }

            layer.Selected.Value = true;

            if (LayerSelectionChanged != null)
            {
                LayerSelectionChanged();
            }
        }
    }
}

