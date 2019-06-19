using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

public static class TexturePacker
{
    private class SpriteFrame
    {
        public Point offset;
        public Rectangle rect;
        public Size original_size;
        public bool rotated = false;
    }

    private static bool ExtractSinglePlist(string plist_file_path)
    {
        Console.WriteLine(string.Format("Extracting plist file: {0} ...", plist_file_path));

        var dict = Plist.Load(plist_file_path);

        // check is texturepacker plist file
        if (dict["metadata"].IsNull()) {
            Console.WriteLine("is not texturepacker plist file.");
            return false;
        }

        // check format
        var format = dict["metadata"]["format"].integer_value;
        if (format < 0 || format > 3) {
            Console.WriteLine(string.Format("format = {0} is not supported.", format));
            return false;
        }

        // check png file
        var path = Path.GetDirectoryName(plist_file_path);
        var texture_path = dict["metadata"]["textureFileName"].string_value;
        if (!string.IsNullOrEmpty(texture_path)) {
            texture_path = Path.Combine(path, texture_path);
        }

        var plist_file_name = Path.GetFileNameWithoutExtension(plist_file_path);
        if (string.IsNullOrEmpty(texture_path) || !File.Exists(texture_path)) {
            texture_path = Path.Combine(path, plist_file_name + ".png");
        }

        if (!File.Exists(texture_path)) {
            Console.WriteLine(string.Format("texture file ({0}) not found.", texture_path));
            return false;
        }

        // generate all sprite frames
        var frames = new Dictionary<string, SpriteFrame>();
        foreach (var kvp in dict["frames"]) {
            var name = kvp.Key;
            var frame = kvp.Value;

            if (format == 0) {
                var x = frame["x"].real_value;
                var y = frame["y"].real_value;
                var w = frame["width"].real_value;
                var h = frame["height"].real_value;
                var ox = frame["offsetX"].real_value;
                var oy = frame["offsetY"].real_value;
                var ow = frame["originalWidth"].integer_value;
                var oh = frame["originalHeight"].integer_value;

                ow = Math.Abs(ow);
                oh = Math.Abs(oh);
                frames.Add(name, new SpriteFrame()
                {
                    rect = new Rectangle((int)x, (int)y, (int)w, (int)h),
                    rotated = false,
                    offset = new Point((int)ox, (int)oy),
                    original_size = new Size(ow, oh)
                });
            }
            else if (format == 1 || format == 2) {
                var rotated = false;
                if (format == 2) {
                    rotated = frame["rotated"].bool_value;
                }

                var sprite_frame = new SpriteFrame()
                {
                    rect = frame["frame"].string_value.ToRectangle(),
                    rotated = rotated,
                    offset = frame["offset"].string_value.ToPoint(),
                    original_size = frame["sourceSize"].string_value.ToSize()
                };

                if (rotated) {
                    sprite_frame.rect = sprite_frame.rect.SwapSize();
                    sprite_frame.offset = sprite_frame.offset.Swap();
                    sprite_frame.original_size = sprite_frame.original_size.Swap();
                }

                frames.Add(name, sprite_frame);
            }
            else if (format == 3) {
                var sprite_size = frame["spriteSize"].string_value.ToSize();
                var sprite_offset = frame["spriteOffset"].string_value.ToPoint();
                var sprite_source_size = frame["spriteSourceSize"].string_value.ToSize();
                var texture_rect = frame["textureRect"].string_value.ToRectangle();
                var rotated = frame["textureRotated"].bool_value;

                var sprite_frame = new SpriteFrame()
                {
                    rect = new Rectangle(texture_rect.X, texture_rect.Y, sprite_size.Width, sprite_size.Height),
                    rotated = rotated,
                    offset = sprite_offset,
                    original_size = sprite_source_size
                };

                if (rotated) {
                    sprite_frame.rect = sprite_frame.rect.SwapSize();
                    sprite_frame.offset = sprite_frame.offset.Swap();
                    sprite_frame.original_size = sprite_frame.original_size.Swap();
                }

                frames.Add(name, sprite_frame);

                var aliases = frame["aliases"];
                if (!aliases.IsNull()) {
                    for (var i = 0; i < aliases.Count; ++i) {
                        var aliase_name = aliases[i].string_value;
                        frames.Add(aliase_name, sprite_frame);
                    }
                }
            }
        }

        // create directory for extracted images
        var sub_path = Path.Combine(path, plist_file_name);
        Directory.CreateDirectory(sub_path);

        // extract all sprite frame images
        var altas = Bitmap.FromFile(texture_path);
        foreach (var kvp in frames) {
            var frame = kvp.Value;

            var width = frame.rect.Size.Width;
            var height = frame.rect.Size.Height;

            var real_width = frame.original_size.Width;
            var real_height = frame.original_size.Height;

            var offset_x = frame.offset.X;
            var offset_y = frame.offset.Y;

            var image = new Bitmap(real_width, real_height);
            var result_rect = new Rectangle()
            {
                X = (real_width - width) / 2 + offset_x,
                Y = (real_height - height) / 2 + (frame.rotated ? offset_y : -offset_y),
                Width = width,
                Height = height
            };

            using (var graphic = Graphics.FromImage(image)) {
                graphic.DrawImage(altas, result_rect, frame.rect, GraphicsUnit.Pixel);
            }

            if (frame.rotated) {
                image.RotateFlip(RotateFlipType.Rotate270FlipNone);
            }

            var output_path = Path.Combine(sub_path, kvp.Key);
            image.Save(output_path);
            Console.WriteLine(string.Format("Save at {0}", output_path));
        }
        return true;
    }

    public static void Extract(string path)
    {
        var attr = File.GetAttributes(path);
        if (attr.HasFlag(FileAttributes.Directory)) {
            Console.WriteLine("Extract at " + path);

            var dir = new DirectoryInfo(path);
            foreach (var plist_file in dir.GetFiles("*.plist")) {
                ExtractSinglePlist(plist_file.FullName);
            }
        }
        else {
            if (Path.GetExtension(path) == ".plist") {
                ExtractSinglePlist(path);
            }
        }
    }
}

public static class Extension
{
    public static Point ToPoint(this string content)
    {
        var lst = content.Substring(1, content.Length - 2).Split(',');
        if (lst.Length == 2) {
            return new Point()
            {
                X = int.Parse(lst[0].Trim()),
                Y = int.Parse(lst[1].Trim())
            };
        }

        return Point.Empty;
    }

    public static Size ToSize(this string content)
    {
        var lst = content.Substring(1, content.Length - 2).Split(',');
        if (lst.Length == 2) {
            return new Size()
            {
                Width = int.Parse(lst[0].Trim()),
                Height = int.Parse(lst[1].Trim())
            };
        }
        return Size.Empty;
    }

    public static Rectangle ToRectangle(this string content)
    {
        content = content.Substring(1, content.Length - 2);
        var split_idx = content.IndexOf(',', content.IndexOf('}'));
        var point = content.Substring(0, split_idx);
        var size = content.Substring(split_idx + 1);
        return new Rectangle()
        {
            Location = point.Trim().ToPoint(),
            Size = size.Trim().ToSize()
        };
    }

    public static Point Swap(this Point point)
    {
        var temp = point.X;
        point.X = point.Y;
        point.Y = temp;

        return point;
    }

    public static Size Swap(this Size size)
    {
        var temp = size.Width;
        size.Width = size.Height;
        size.Height = temp;

        return size;
    }

    public static Rectangle SwapSize(this Rectangle rect)
    {
        var temp = rect.Width;
        rect.Width = rect.Height;
        rect.Height = temp;

        return rect;
    }
}