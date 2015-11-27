/**
 * @file
 * The Tinify API allows you to compress and optimize JPEG and PNG images.
 * 
 * @author
 * Stian Hanger <pdnagilum@gmail.com>
 * 
 * @documentation
 * https://tinypng.com/developers
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

/// <summary>
/// The Tinify API allows you to compress and optimize JPEG and PNG images.
/// </summary>
public class Tinify {
	/// <summary>
	/// Base 64 encoded API key.
	/// </summary>
	private string base64ApiKey { get; set; }

	/// <summary>
	/// The Tinify API allows you to compress and optimize JPEG and PNG images.
	/// </summary>
	/// <param name="apiKey"></param>
	public Tinify(string apiKey) {
		this.base64ApiKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));
	}

	/// <summary>
	/// Scales the image proportionally and crops it if necessary so that the result has exactly the given dimensions. You must provide both a width and a height.
	/// </summary>
	/// <param name="response">Response object from a upload/shrink operation.</param>
	/// <param name="width">Width to scale within.</param>
	/// <param name="height">Height to scale within.</param>
	/// <param name="outputFile">Filename to save to.</param>
	/// <param name="store">Amazon S3 credentials and information.</param>
	/// <returns>Stream</returns>
	public Stream Cover(TinifyResponse response, int width, int height, string outputFile = null, TinifyOptionsStore store = null) {
		return executeOption(response, width, height, "cover", outputFile, store);
	}

	/// <summary>
	/// Scales the image down proportionally so that it fits within the given dimensions. You must provide both a width and a height.
	/// </summary>
	/// <param name="response">Response object from a upload/shrink operation.</param>
	/// <param name="width">Width to scale within.</param>
	/// <param name="height">Height to scale within.</param>
	/// <param name="outputFile">Filename to save to.</param>
	/// <param name="store">Amazon S3 credentials and information.</param>
	/// <returns>Stream</returns>
	public Stream Fit(TinifyResponse response, int width, int height, string outputFile = null, TinifyOptionsStore store = null) {
		return executeOption(response, width, height, "fit", outputFile, store);
	}

	/// <summary>
	/// Scales the image down proportionally. You must provide either a target width or a target height, but not both.
	/// </summary>
	/// <param name="response">Response object from a upload/shrink operation.</param>
	/// <param name="width">Width to scale within.</param>
	/// <param name="height">Height to scale within.</param>
	/// <param name="outputFile">Filename to save to.</param>
	/// <param name="store">Amazon S3 credentials and information.</param>
	/// <returns>Stream</returns>
	public Stream Scale(TinifyResponse response, int width, int height, string outputFile = null, TinifyOptionsStore store = null) {
		return executeOption(response, width, height, "scale", outputFile, store);
	}
	
	/// <summary>
	/// Upload image to Tinify and attempt to shrink it, lossless.
	/// </summary>
	/// <param name="inputFile">File to upload.</param>
	/// <param name="outputFile">Filename to save to.</param>
	/// <param name="store">Amazon S3 credentials and information.</param>
	/// <returns>JSON with info about the upload/shrink.</returns>
	public TinifyResponse Shrink(string inputFile, string outputFile = null, TinifyOptionsStore store = null) {
		var response = new JavaScriptSerializer()
			.Deserialize<TinifyResponse>(
				getResponse(request(binaryFile: inputFile)));

		if (outputFile == null ||
			response.output == null ||
			string.IsNullOrEmpty(response.output.url))
			return response;

		if (store != null)
			executeOption(
				response,
				null,
				null,
				null,
				null,
				store);

		var webClient = new WebClient();

		webClient.DownloadFile(
			response.output.url,
			outputFile);

		return response;
	}

	/// <summary>
	/// Execute a set of options against a previous uploaded/shrinked file.
	/// </summary>
	/// <param name="response">Response object from a upload/shrink operation.</param>
	/// <param name="width">Width to scale within.</param>
	/// <param name="height">Height to scale within.</param>
	/// <param name="method">Scale method to apply.</param>
	/// <param name="outputFile">Filename to save to.</param>
	/// <param name="store">Amazon S3 credentials and information.</param>
	/// <returns>Stream</returns>
	private Stream executeOption(TinifyResponse response, int? width, int? height, string method = null, string outputFile = null, TinifyOptionsStore store = null) {
		var options = new TinifyOptions();

		if (method != null &&
		    (width.HasValue ||
		     height.HasValue)) {
			options.resize = new TinifyOptionsResize {
				method = method
			};

			if (width.HasValue)
				options.resize.width = width.Value;

			if (height.HasValue)
				options.resize.height = height.Value;
		}

		if (store != null)
			options.store = store;

		var stream = request(
			"POST",
			response.output.url,
			null,
			options);

		writeStreamToDisk(
			stream,
			outputFile);

		return stream;
	}

	/// <summary>
	/// Get the response as a string.
	/// </summary>
	/// <param name="responseStream">Stream to read from.</param>
	/// <returns>String</returns>
	private string getResponse(Stream responseStream) {
		if (responseStream == null)
			return null;

		var output = new List<byte>();
		var buffer = new byte[1024];
		int byteCount;

		do {
			byteCount = responseStream.Read(buffer, 0, buffer.Length);

			for (var i = 0; i < byteCount; i++)
				output.Add(buffer[i]);

		} while (byteCount > 0);

		return Encoding.UTF8.GetString(output.ToArray());
	}

	/// <summary>
	/// Performs the actual communication towards the Tinify API.
	/// </summary>
	/// <param name="method">HTTP method to perform.</param>
	/// <param name="url">URL to request.</param>
	/// <param name="binaryFile">File to upload.</param>
	/// <param name="options">Options to pass along to the API.</param>
	/// <returns>Stream</returns>
	private Stream request(string method = "POST", string url = null, string binaryFile = null, TinifyOptions options = null) {
		if (url == null)
			url = "https://api.tinify.com/shrink";

		var request = WebRequest.Create(url) as HttpWebRequest;

		if (request == null)
			throw new WebException("Could not create webrequest.");

		request.Method = method;
		request.Headers.Add(
			"Authorization",
			"Basic " + this.base64ApiKey);

		if (!string.IsNullOrEmpty(binaryFile)) {
			var requestStream = request.GetRequestStream();
			var bytes = File.ReadAllBytes(binaryFile);
			requestStream.Write(bytes, 0, bytes.Length);
		}

		if (options != null) {
			request.ContentType = "application/json";

			var requestStream = request.GetRequestStream();
			var json = new JavaScriptSerializer().Serialize(options);
			var bytes = Encoding.UTF8.GetBytes(json);

			requestStream.Write(bytes, 0, bytes.Length);
		}

		var response = request.GetResponse() as HttpWebResponse;

		if (response == null)
			throw new WebException("Request returned NULL response.");

		return response.GetResponseStream();
	}

	/// <summary>
	/// Write a stream to disk.
	/// </summary>
	/// <param name="stream">Stream to write.</param>
	/// <param name="outputFile">File to write too.</param>
	private void writeStreamToDisk(Stream stream, string outputFile) {
		if (outputFile == null)
			return;

		if (stream == null)
			return;

		var output = new List<byte>();
		var buffer = new byte[1024];
		int byteCount;

		do {
			byteCount = stream.Read(buffer, 0, buffer.Length);

			for (var i = 0; i < byteCount; i++)
				output.Add(buffer[i]);

		} while (byteCount > 0);

		using (var fileStream = File.Create(outputFile))
			fileStream.Write(output.ToArray(), 0, output.Count);
	}
}

public class TinifyResponse {
	public TinifyResponseInput input { get; set; }
	public TinifyResponseOutput output { get; set; }
	public string error { get; set; }
	public string message { get; set; }
}

public class TinifyResponseInput {
	public int size { get; set; }
	public string type { get; set; }
}

public class TinifyResponseOutput {
	public int size { get; set; }
	public string type { get; set; }
	public int width { get; set; }
	public int height { get; set; }
	public decimal ratio { get; set; }
	public string url { get; set; }
}

public class TinifyOptions {
	public TinifyOptionsResize resize { get; set; }
	public TinifyOptionsStore store { get; set; }
}

public class TinifyOptionsResize {
	public string method { get; set; }
	public int width { get; set; }
	public int height { get; set; }
}

public class TinifyOptionsStore {
	public string service = "s3";
	public string aws_access_key_id { get; set; }
	public string aws_secret_access_key { get; set; }
	public string region { get; set; }
	public string path { get; set; }
}