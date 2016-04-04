﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitana.Framework.Ui.Controllers;
using Sitana.Framework.Ui.DefinitionFiles;
using Sitana.Framework.Xml;
using Microsoft.Xna.Framework.Graphics;
using Sitana.Framework.Graphics;
using Microsoft.Xna.Framework;
using Sitana.Framework.Ui.Core;

namespace Sitana.Framework.Ui.Views.ButtonDrawables
{
    public class Image : ButtonDrawable
    {
        public new static void Parse(XNode node, DefinitionFile file)
        {
            ButtonDrawable.Parse(node, file);

            var parser = new DefinitionParser(node);

            file["Image"] = parser.ParseResource<Texture2D>("Image");
            file["Scale"] = parser.ParseScale("Scale");
            file["HorizontalContentAlignment"] = parser.ParseEnum<HorizontalContentAlignment>("HorizontalContentAlignment");
            file["VerticalContentAlignment"] = parser.ParseEnum<VerticalContentAlignment>("VerticalContentAlignment");
        }

        protected Texture2D _image = null;
        protected Scale _scale;

        protected HorizontalContentAlignment _horizontalAlignment;
        protected VerticalContentAlignment _verticalAlignment;

        protected override void Init(UiController controller, object binding, DefinitionFile definition)
        {
            base.Init(controller, binding, definition);

            DefinitionFileWithStyle file = new DefinitionFileWithStyle(definition, typeof(Image));

            _image = DefinitionResolver.Get<Texture2D>(controller, binding, file["Image"], null);
            _scale = DefinitionResolver.Get(controller, binding, file["Scale"], Scale.One);
            _horizontalAlignment = DefinitionResolver.Get<HorizontalContentAlignment>(controller, binding, file["HorizontalContentAlignment"], HorizontalContentAlignment.Center);
            _verticalAlignment = DefinitionResolver.Get<VerticalContentAlignment>(controller, binding, file["VerticalContentAlignment"], VerticalContentAlignment.Center);
        }

        public override void Draw(AdvancedDrawBatch drawBatch, DrawButtonInfo info)
        {
            Update(info.EllapsedTime, info.ButtonState);

            Color color = ColorFromState * info.Opacity * Opacity;

            Texture2D image = _image;

            if (image != null)
            {
                float scale = _scale.Value(true);

                if (scale == 0)
                {
                    scale = Math.Min(info.Target.Width / (float)image.Width, info.Target.Height / (float)image.Height);
                    scale = Math.Min(1, scale);
                }

                Rectangle textureSrc = new Rectangle(0, 0, image.Width, image.Height);

                int width = (int)(scale * image.Width);
                int height = (int)(scale * image.Height);

                Rectangle target = _margin.ComputeRect(info.Target);

                switch ( _horizontalAlignment )
                {
                case HorizontalContentAlignment.Center:
                    target.X = target.Center.X - width / 2;
                    break;

                case HorizontalContentAlignment.Right:
                    target.X = target.Right - width;
                    break;
                }

                switch ( _verticalAlignment)
                {
                case VerticalContentAlignment.Center:
                    target.Y = target.Center.Y - height / 2;
                    break;

                case VerticalContentAlignment.Bottom:
                    target.Y = target.Bottom - height;
                    break;
                }

                target.Width = width;
                target.Height = height;

                drawBatch.DrawImage(image, target, textureSrc, color);
            }
        }
    }
}
