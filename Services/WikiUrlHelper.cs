using System;
using System.Text;
using Gw2EventTracker.Models;

namespace Gw2EventTracker.Services {

    public static class WikiUrlHelper {

        private const string WikiBase = "https://wiki.guildwars2.com/wiki/";

        public static string ResolveWikiUrl(EventSectionDefinition section, EventSegmentDefinition segment) {
            var pageTitle = FirstNonEmpty(segment.Link, section.Link, segment.Name);
            return BuildWikiUrl(pageTitle);
        }

        private static string FirstNonEmpty(params string[] values) {
            foreach (var value in values) {
                if (!string.IsNullOrWhiteSpace(value)) {
                    return value;
                }
            }

            return string.Empty;
        }

        public static string BuildWikiUrl(string pageTitle) {
            if (string.IsNullOrWhiteSpace(pageTitle)) {
                return string.Empty;
            }

            var hashIndex = pageTitle.IndexOf('#');
            string page;
            string? anchor;
            if (hashIndex >= 0) {
                page = pageTitle.Substring(0, hashIndex).Trim();
                anchor = pageTitle.Substring(hashIndex + 1);
            } else {
                page = pageTitle.Trim();
                anchor = null;
            }

            if (string.IsNullOrEmpty(page)) {
                return string.Empty;
            }

            var url = WikiBase + EncodePageTitle(page);
            if (!string.IsNullOrEmpty(anchor)) {
                url += "#" + anchor;
            }

            return url;
        }

        private static string EncodePageTitle(string page) {
            var builder = new StringBuilder(page.Length);
            foreach (var character in page) {
                if (character == ' ') {
                    builder.Append('_');
                } else if (IsAllowedWikiCharacter(character)) {
                    builder.Append(character);
                } else {
                    builder.Append(Uri.EscapeDataString(character.ToString()));
                }
            }

            return builder.ToString();
        }

        private static bool IsAllowedWikiCharacter(char character) {
            return char.IsLetterOrDigit(character)
                || character == '_'
                || character == '-'
                || character == '.'
                || character == ':'
                || character == '('
                || character == ')'
                || character == '\''
                || character == '!';
        }
    }

}