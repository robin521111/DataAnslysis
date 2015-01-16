using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace premiere.Data.Helper
{
    public static class ButtonExtensions
    {
        //public static string IconButton(this HtmlHelper helper, string id, string className, string insideText, string text)
        //{
        //    return IconButton(helper,id, className, insideText, text);

        //}
        /// <summary>
        /// this is a close icon button X
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="action">action for close button</param>
        /// <param name="routeValues"></param>
        /// <param name="className">class for css</param>
        /// <param name="text">inner text</param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString IconButton(this HtmlHelper helper, string action, object routeValues, int bid, object htmlAttributes)
        {
            //instantiate a UrlHelper
            var urlhelper = new UrlHelper(helper.ViewContext.RequestContext);

            var builder = new TagBuilder("button");

            RouteValueDictionary rv = new RouteValueDictionary();
            rv.Add("bid", bid);

            // builder.MergeAttribute("submit", urlhelper.Action(action, routeValues));
            builder.MergeAttribute("onclick", "window.location.href=\' " + urlhelper.Action("DeleteBlog", "Blog", new { bid = bid }) + "\'");

            // builder.MergeAttribute("onclick", "window.location.href='/Blog/DeleteBlog/"+bid.ToString()+ "'");
            builder.MergeAttribute("class", "close");
            builder.InnerHtml = "<span aria-hidden='true'>&times;</span><span class='sr-only'>Close</span>";
            if (htmlAttributes != null)
            {
                builder.MergeAttributes(new RouteValueDictionary(htmlAttributes), true);
            }
            string buttonHtml = builder.ToString(TagRenderMode.Normal);
            return MvcHtmlString.Create(buttonHtml);
        }



        public static MvcHtmlString IconActionLink(this HtmlHelper helper, string action, string text, object routeValues, string iconClass, bool span, object htmlAttributes)
        {

            var urlhelper = new UrlHelper(helper.ViewContext.RequestContext);

            var builder = new TagBuilder("a");

            builder.MergeAttribute("href", urlhelper.Action(action, routeValues));



            builder.InnerHtml = "<i class='" + iconClass + "'></i>";

            builder.InnerHtml += text;

            if (span)
            {
                string spanstr = "<span class='fa arrow'></span>";
                builder.InnerHtml += spanstr;
            }

            string IconLinkHtml = builder.ToString(TagRenderMode.Normal);

            return MvcHtmlString.Create(IconLinkHtml);

        }


        public static MvcHtmlString InputButton(this HtmlHelper html, string cla, string value, object routeValues)
        {
            var url = new UrlHelper(html.ViewContext.RequestContext);
            //build the <input> tag
            var inputBuilder = new TagBuilder("input");
            inputBuilder.MergeAttribute("type", value);
            inputBuilder.MergeAttribute("class", cla);
            string inputHtml = inputBuilder.ToString(TagRenderMode.SelfClosing);
            return MvcHtmlString.Create(inputHtml);
        }



        public static MvcHtmlString ActionImage(this HtmlHelper html, string action, object routeValues, string imagePath, string alt)
        {
            var url = new UrlHelper(html.ViewContext.RequestContext);
            //build the <img> tag
            var imgBuilder = new TagBuilder("img");
            imgBuilder.MergeAttribute("src", url.Content(imagePath));
            imgBuilder.MergeAttribute("alt", alt);
            string imgHtml = imgBuilder.ToString(TagRenderMode.SelfClosing);
            //build the <a> tag
            var anchorBuilder = new TagBuilder("a");
            anchorBuilder.MergeAttribute("href", url.Action(action, routeValues));
            anchorBuilder.InnerHtml = imgHtml;
            string anchorHtml = anchorBuilder.ToString(TagRenderMode.Normal);
            return MvcHtmlString.Create(anchorHtml);
        }

    }

}
