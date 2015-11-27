# Tinify

C# wrapper for the Tinify (TinyPNG) API.

The Tinify API allows you to compress and optimize JPEG and PNG images.

**Usage**

    // Init a new instance of the class.
    var tinify = new Tinify("--your-api-key-from-tiny-png--");

    // Upload and shrink an image.
    var resp = tinify.Shrink(
        Server.MapPath("~/wallhaven-88501.jpg"),
        Server.MapPath("~/wallhaven-88501-tinify.jpg"));

    // Create a cover with the uploaded image.
    tinify.Cover(resp, 200, 200, Server.MapPath("~/wallhaven-88501-tinify-cover-200x200.jpg"));

    // Create a thumbnail with the uploaded image.
    tinify.Fit(resp, 200, 200, Server.MapPath("~/wallhaven-88501-tinify-fit-200x200.jpg"));

    // Scale the uploaded image.
    tinify.Scale(resp, 0, 200, Server.MapPath("~/wallhaven-88501-tinify-scale-200x200.jpg"));