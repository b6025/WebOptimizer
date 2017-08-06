﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NUglify;
using NUglify.Html;

namespace WebOptimizer
{
    internal class HtmlMinifier : IProcessor
    {
        public HtmlMinifier(HtmlSettings settings)
        {
            Settings = settings;
        }

        public string CacheKey(HttpContext context) => string.Empty;

        public HtmlSettings Settings { get; set; }

        public Task ExecuteAsync(IAssetContext config)
        {
            var content = new Dictionary<string, byte[]>();

            foreach (string key in config.Content.Keys)
            {
                if (key.EndsWith(".min.html"))
                    continue;

                string input = config.Content[key].AsString();
                UglifyResult result = Uglify.Html(input, Settings);
                string minified = result.Code;

                if (result.HasErrors)
                {
                    minified = $"<!-- {string.Join("\r\n", result.Errors)} -->\r\n" + input;
                }

                content[key] = minified.AsByteArray();
            }

            config.Content = content;

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="IAssetPipeline"/>.
    /// </summary>
    public static partial class PipelineExtensions
    {
        /// <summary>
        /// Minifies and fingerprints any .html file requested.
        /// </summary>
        public static IAsset MinifyHtmlFiles(this IAssetPipeline pipeline) =>
            pipeline.MinifyHtmlFiles(new HtmlSettings());

        /// <summary>
        /// Minifies and fingerprints any .html file requested.
        /// </summary>
        public static IAsset MinifyHtmlFiles(this IAssetPipeline pipeline, HtmlSettings settings)
        {
            return pipeline.AddFileExtension(".html", "text/html; charset=UTF-8")
                           .MinifyHtml(settings);
        }


        /// <summary>
        /// Minifies the specified .html files
        /// </summary>
        public static IEnumerable<IAsset> MinifyHtmlFiles(this IAssetPipeline pipeline, params string[] sourceFiles)
        {
            return pipeline.MinifyHtmlFiles(new HtmlSettings(), sourceFiles);
        }

        /// <summary>
        /// Minifies the specified .html files
        /// </summary>
        public static IEnumerable<IAsset> MinifyHtmlFiles(this IAssetPipeline pipeline, HtmlSettings settings, params string[] sourceFiles)
        {
            return pipeline.AddFiles("text/html; charset=UTF-8", sourceFiles)
                           .MinifyHtml(settings);
        }

        /// <summary>
        /// Creates a HTML bundle on the specified route and minifies the output.
        /// </summary>
        public static IAsset AddHtmlBundle(this IAssetPipeline pipeline, string route, params string[] sourceFiles)
        {
            return pipeline.AddHtmlBundle(route, new HtmlSettings(), sourceFiles);
        }

        /// <summary>
        /// Creates a HTML bundle on the specified route and minifies the output.
        /// </summary>
        public static IAsset AddHtmlBundle(this IAssetPipeline pipeline, string route, HtmlSettings settings, params string[] sourceFiles)
        {
            return pipeline.AddBundle(route, "text/html; charset=UTF-8", sourceFiles)
                           .Concatinate()
                           .MinifyHtml(settings);
        }

        /// <summary>
        /// Runs the HTML minifier on the content.
        /// </summary>
        public static IAsset MinifyHtml(this IAsset bundle)
        {
            return bundle.MinifyHtml(new HtmlSettings());
        }

        /// <summary>
        /// Runs the HTML minifier on the content.
        /// </summary>
        public static IAsset MinifyHtml(this IAsset bundle, HtmlSettings settings)
        {
            var minifier = new HtmlMinifier(settings);
            bundle.Processors.Add(minifier);

            return bundle;
        }

        /// <summary>
        /// Runs the HTML minifier on the content.
        /// </summary>
        public static IEnumerable<IAsset> MinifyHtml(this IEnumerable<IAsset> assets)
        {
            return assets.MinifyHtml(new HtmlSettings());
        }

        /// <summary>
        /// Runs the HTML minifier on the content.
        /// </summary>
        public static IEnumerable<IAsset> MinifyHtml(this IEnumerable<IAsset> assets, HtmlSettings settings)
        {
            var list = new List<IAsset>();

            foreach (IAsset asset in assets)
            {
                list.Add(asset.MinifyHtml(settings));
            }

            return list;
        }
    }
}