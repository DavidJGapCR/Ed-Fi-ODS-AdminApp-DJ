﻿// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApp.Management;
using EdFi.Ods.AdminApp.Web.Infrastructure.Tags;
using HtmlTags;
using HtmlTags.Conventions;
using HtmlTags.Conventions.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using EdFi.Ods.AdminApp.Web.Display.RadioButton;
using EdFi.Ods.AdminApp.Web.Display.TabEnumeration;
using EdFi.Ods.AdminApp.Web.Infrastructure;
using HtmlTags.Reflection;
using Property = EdFi.Ods.AdminApp.Web.Infrastructure.Property;
using Preconditions = EdFi.Ods.Common.Preconditions;

namespace EdFi.Ods.AdminApp.Web.Helpers
{
    public static class HtmlHelperExtensions
    {
        private static readonly HtmlConventionLibrary HtmlConventionLibrary = OdsAdminHtmlConventionLibrary.CreateHtmlConventionLibrary();

        private static IElementGenerator<T> GetGenerator<T>(T model) where T : class
        {
            return ElementGenerator<T>.For(HtmlConventionLibrary, null, model);
        }

        public static HtmlTag Input<T>(this HtmlHelper<T> helper, Expression<Func<T, object>> expression) where T : class
        {
            var generator = GetGenerator(helper.ViewData.Model);
            return generator.InputFor(expression);
        }

        public static HtmlTag FileInputBlock<T>(this HtmlHelper<T> helper, Expression<Func<T, object>> expression) where T : class
        {
            var accept = Property.From(expression).GetCustomAttributes<AcceptAttribute>().SingleOrDefault();

            Action<HtmlTag> action = input =>
            {
                input.Attr("type", "file");
                input.AddClass("form-control");
                if (accept != null)
                    input.Attr("accept", accept.FileTypeSpecifier);
            }; 

            return helper.InputBlock(expression, null, null, action);
        }

        public static HtmlTag Label<T>(this HtmlHelper<T> helper, Expression<Func<T, object>> expression) where T : class
        {
            var generator = GetGenerator(helper.ViewData.Model);
            return generator.LabelFor(expression);
        }

        public static HtmlTag ToolTip<T>(this HtmlHelper<T> helper, string helpTooltipText)
        {
            var helpTooltip = new HtmlTag("span");
            if (!string.IsNullOrEmpty(helpTooltipText))
            {
                helpTooltip
                    .Attr("title", helpTooltipText)
                    .Data("toggle", "tooltip")
                    .AddClasses("fa", "fa-question-circle-o", "form-icons");
            }
            helpTooltip = helpTooltip.WrapWith(new HtmlTag("span").AddClasses("text-left", "help-form"));

            return helpTooltip;
        }

        private static HtmlTag FormBlock(HtmlTag label, HtmlTag input, HtmlTag toolTip)
        {
            var formRow = new DivTag().AddClasses("row", "form-group");
            formRow.Append(label);
            formRow.Append(input);
            formRow.Append(toolTip);

            var wrapper = new DivTag().AddClass("form-horizontal");
            wrapper.Append(formRow);

            return wrapper;
        }

        public static HtmlTag NumberOnlyInputBlock<T>(this HtmlHelper<T> helper, Expression<Func<T, object>> expression, string placeholderText = null, string helpTooltipText = null, string customLabelText = null, int maxValue = int.MaxValue, int minValue=0) where T : class
        {
            void Action(HtmlTag input)
            {
                input.Attr("type", "number");
                input.Attr("max", maxValue);
                input.Attr("min", minValue);
                input.AddClass("form-control");
            }

            return helper.InputBlock(expression, placeholderText, helpTooltipText, Action, customLabelText);
        }

        public static HtmlTag InputBlock<T>(this HtmlHelper<T> helper, Expression<Func<T, object>> expression, string placeholderText = null, string helpTooltipText = null, Action<HtmlTag> inputModifier = null, string customLabelText = null) where T : class
        {
            Preconditions.ThrowIfNull(helper, nameof(helper));
            Preconditions.ThrowIfNull(expression, nameof(expression));

            var label = helper.Label(expression);
            if (customLabelText != null)
            {
                label.Text(customLabelText);
            }
            label = label.WrapWith(new DivTag().AddClasses("col-xs-4", "text-right"));

            var input = helper.Input(expression);
            if (!string.IsNullOrEmpty(placeholderText))
            {
                input.AddPlaceholder(placeholderText);
            }
            inputModifier?.Invoke(input);

            var isCheckbox = expression?.ToAccessor()?.PropertyType == typeof(bool);

            input = input.WrapWith(new DivTag().AddClasses("col-xs-6", isCheckbox ? "text-left" : "text-center"));
            

            var helpTooltip = helper.ToolTip(helpTooltipText);
            helpTooltip = helpTooltip.AddClasses("col-xs-2");

            return FormBlock(label, input, helpTooltip);
        }

        public static HtmlTag SelectList<T, TR>(this HtmlHelper<T> helper, Expression<Func<T, TR>> expression, bool includeBlankOption = false)
            where T : class
            where TR : Enumeration<TR>
        {
            var getAllMethod = typeof (TR).GetMethod("GetAll", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            var enumerationValues = (IEnumerable<TR>)getAllMethod.Invoke(null, null);

            var model = helper.ViewData.Model;
            var expressionValue = expression.Compile().Invoke(model);
            var convertedExpression = expression.Cast<T, TR, object>();

            return helper.SelectList(convertedExpression, enumerationValues, i => new SelectListItem { Text = i.DisplayName, Value = i.Value.ToString(), Selected = i == expressionValue}, includeBlankOption);
        }

        public static HtmlTag SelectList<T, TR>(this HtmlHelper<T> helper, Expression<Func<T, object>> expression, IEnumerable<TR> options, Func<TR, SelectListItem> selectListItemBuilder, bool includeBlankOption = false) where T : class
        {
            var input = helper.Input(expression).TagName("select").RemoveAttr("value");

            if (includeBlankOption)
            {
                var blankSelectListItem = new SelectListItem
                {
                    Text = "",
                    Value = ""
                };

                AppendSelectListOption(blankSelectListItem, input);
            }

            foreach (var selectListItem in options.Select(selectListItemBuilder))
            {
                AppendSelectListOption(selectListItem, input);
            }

            return input;
        }

        private static void AppendSelectListOption(SelectListItem selectListItem, HtmlTag selectTag)
        {
            var optionTag = new HtmlTag("option").Attr("value", selectListItem.Value).Text(selectListItem.Text);
            if (selectListItem.Selected)
            {
                optionTag.Attr("selected", "selected");
            }

            selectTag.Append(optionTag);
        }

        public static HtmlTag SelectListBlock<T, TR>(this HtmlHelper<T> helper, Expression<Func<T, object>> expression, IEnumerable<TR> options, Func<TR, SelectListItem> selectListItemBuilder, string helpTooltipText = null, bool includeBlankOption = false) where T: class
        {
            var selectList = SelectList(helper, expression, options, selectListItemBuilder, includeBlankOption);
            return helper.SelectListBlock(expression, selectList, helpTooltipText, includeBlankOption);
        }

        public static HtmlTag SelectListBlock<T, TR>(this HtmlHelper<T> helper, Expression<Func<T, TR>> expression, string helpTooltipText = null, bool includeBlankOption = false) 
            where T : class
            where TR: Enumeration<TR>
        {
            var selectList = SelectList(helper, expression, includeBlankOption);
            var convertedExpression = expression.Cast<T, TR, object>();

            return helper.SelectListBlock(convertedExpression, selectList, helpTooltipText, includeBlankOption);
        }

        public static HtmlTag SelectListBlock<T>(this HtmlHelper<T> helper, Expression<Func<T, object>> expression, HtmlTag selectList, string helpTooltipText = null, bool includeBlankOption = false)
            where T : class
        {
            var label = helper.Label(expression);
            label = label.WrapWith(new DivTag().AddClasses("col-xs-4", "text-right"));

            var input = selectList;
            input = input.WrapWith(new DivTag().AddClasses("col-xs-6", "text-left"));

            var helpTooltip = helper.ToolTip(helpTooltipText);
            helpTooltip = helpTooltip.AddClasses("col-xs-2");

            return FormBlock(label, input, helpTooltip);
        }

        public static HtmlTag MultiSelectList<T, TR>(this HtmlHelper<T> helper, Expression<Func<T, object>> expression,
            IEnumerable<TR> options,
            Func<TR, SelectListItem> selectListItemBuilder) where T : class
        {
            var input = helper.SelectList(expression, options, selectListItemBuilder);
            input.Attr("multiple", "multiple");

            return input;
        }

        public static HtmlTag MultiSelectListBlock<T, TR>(this HtmlHelper<T> helper,
            Expression<Func<T, object>> expression,
            IEnumerable<TR> options, 
            Func<TR, SelectListItem> selectListItemBuilder, 
            string helpTooltipText = null
        )
            where T : class
        {
            var label = helper.Label(expression);
            label = label.WrapWith(new DivTag().AddClasses("col-xs-4", "text-right"));

            var input = helper.MultiSelectList(expression, options, selectListItemBuilder);
            input = input.WrapWith(new DivTag().AddClasses("col-xs-6", "text-left"));

            var helpTooltip = helper.ToolTip(helpTooltipText);
            helpTooltip = helpTooltip.AddClasses("col-xs-2");

            return FormBlock(label, input, helpTooltip);
        }

        public static HtmlTag ModalFormButtons<T>(this HtmlHelper<T> helper, string confirmButtonText = "Save Changes", string updateTargetId = "")
        {
            var cancelButton = helper.CancelModalButton();
            var saveButton = helper.SaveModalButton(confirmButtonText, updateTargetId);

            return cancelButton.After(saveButton);
        }

        public static HtmlTag Button<T>(this HtmlHelper<T> helper, string buttonText)
        {
            var button = new HtmlTag("button")
                .Text(buttonText)
                .AddClasses("btn", "btn-primary", "cta");

            return button;
        }

        public static HtmlTag SaveButton<T>(this HtmlHelper<T> helper, string buttonText = "Save Changes", string updateTargetId = "")
        {
            var saveButton = helper.Button(buttonText)
                .Attr("type", "submit");

            if (!string.IsNullOrEmpty(updateTargetId))
            {
                saveButton = saveButton.Data("update-target-id", updateTargetId);
            }

            return saveButton;
        }

        public static HtmlTag SaveModalButton<T>(this HtmlHelper<T> helper, string buttonText = "Save Changes", string updateTargetId = "")
        {
            return helper.SaveButton(buttonText, updateTargetId).Data("confirm", "true");
        }

        public static HtmlTag CancelButton<T>(this HtmlHelper<T> helper, string buttonText = "Cancel")
        {
            return new HtmlTag("button")
                .Text(buttonText)
                .AddClasses("btn", "btn-default");
        }

        public static HtmlTag CancelModalButton<T>(this HtmlHelper<T> helper, string buttonText = "Cancel")
        {
            return helper.CancelButton(buttonText).Data("dismiss", "modal");
        }

        public static HtmlTag ValidationBlock<T>(this HtmlHelper<T> helper)
        {
            return new DivTag().AddClasses("validationSummary", "alert", "alert-danger", "hidden");
        }

        public static HtmlTag NavTabs<T>(this HtmlHelper helper, UrlHelper urlHelper, List<TabDisplay<T>> tabs, object commonRouteValues = null) where T: Enumeration<T>, ITabEnumeration
        {
            var tabTag = new HtmlTag("ul").AddClasses("nav", "nav-tabs");
            BuildNavEntries(urlHelper, tabs, tabTag, commonRouteValues);
            return tabTag;
        }

        public static HtmlTag NavPills<T>(this HtmlHelper helper, UrlHelper urlHelper, List<TabDisplay<T>> tabs, object commonRouteValues = null) where T : Enumeration<T>, ITabEnumeration
        {
            var tabTag = new HtmlTag("ul").AddClasses("nav", "nav-pills", "nav-pills-custom");
            BuildNavEntries(urlHelper, tabs, tabTag, commonRouteValues);
            return tabTag;
        }

        private static void BuildNavEntries<T>(UrlHelper urlHelper, List<TabDisplay<T>> tabs, HtmlTag tabTag, object commonRouteValues) where T : Enumeration<T>, ITabEnumeration
        {
            foreach (var tab in tabs.OrderBy(a => a.Tab.Value))
            {
                var listItem = new HtmlTag("li");
                if (!tab.IsEnabled)
                {
                    listItem.AddClass("disabled");
                }

                if (!tab.IsVisible)
                {
                    listItem.AddClass("hidden");
                }

                if (tab.IsSelected)
                {
                    listItem.AddClass("active");
                    listItem.Append(new HtmlTag("a").Attr("href", "#").Text(tab.Tab.DisplayName));
                }

                else
                {
                    var url = urlHelper.Action(tab.Tab.ActionName, tab.Tab.ControllerName, commonRouteValues);
                    listItem.Append(new HtmlTag("a").Attr("href", url).Text(tab.Tab.DisplayName));
                }

                tabTag.Append(listItem);
            }
        }
        
        public static HtmlTag InlineRadioButton<T, TEnumeration>(this HtmlHelper<T> helper, Expression<Func<T, object>> expression, TEnumeration option, string helpTooltipText = null, string id = null, bool enabled = true) 
            where T: class
            where TEnumeration: Enumeration<TEnumeration> 
        {
            var model = helper.ViewData.Model;
            var value = model == null ? default(TEnumeration) : expression.Compile()(model);

            if (id == null)
            {
                id = Guid.NewGuid().ToString();
            }

            var input = helper.Input(expression)
                .Attr("type", "radio")
                .Attr("value", option.Value)
                .AddClass("radio-inline")
                .Id(id);

            if (option == value)
            {
                input.Attr("checked", "checked");
            }

            if (!enabled)
            {
                input.Attr("disabled", "true");
            }

            var label = new HtmlTag("label")
                .AddClass("radio-inline-label")
                .Text(option.DisplayName)
                .Attr("for", id);

            input.Append(label);

            if (!string.IsNullOrWhiteSpace(helpTooltipText))
            {
                var helpTooltip = helper.ToolTip(helpTooltipText);
                input.Append(helpTooltip);
            }

            var inputContainer = new HtmlTag("span")
                .AddClass("radio-inline-container");

            return input.WrapWith(inputContainer);
        }

        public static HtmlTag InlineCustomRadioButton<T, TEnumeration>(this HtmlHelper<T> helper, Expression<Func<T, object>> expression, RadioButtonDisplay<TEnumeration> option, string id = null)
            where T : class 
            where TEnumeration : Enumeration<TEnumeration>, IRadioButton
        {
            var model = helper.ViewData.Model;
            var value = model == null ? default(TEnumeration) : expression.Compile()(model);

            if (id == null)
            {
                id = Guid.NewGuid().ToString();
            }

            var input = helper.Input(expression)
                .Attr("type", "radio")
                .Attr("value", option.RadioButton.Value)
                .AddClass("radio-inline")
                .Id(id);

            if (option == value)
            {
                input.Attr("checked", "checked");
            }

            if (!option.IsEnabled)
            {
                input.Attr("disabled", "true");
            }

            var label = new HtmlTag("label")
                .AddClass("radio-inline-label")
                .Text(option.RadioButton.DisplayName)
                .Attr("for", id);

            input.Append(label);

            if (!string.IsNullOrWhiteSpace(option.RadioButton.HelpTooltip))
            {
                var helpTooltip = helper.ToolTip(option.RadioButton.HelpTooltip);
                input.Append(helpTooltip);
            }

            var inputContainer = new HtmlTag("span")
                .AddClass("radio-inline-container");

            return input.WrapWith(inputContainer);
        }

        public static HtmlTag ActionAjax(this HtmlHelper helper, string actionName, string controllerName, object routeValues, int minHeight = 0, string placeholderText = "")
        {
            var url = new UrlHelper(helper.ViewContext.RequestContext).Action(actionName, controllerName, routeValues);

            var placeholderTag = new DivTag();
            placeholderTag.Append(new HtmlTag("h6").Text(placeholderText));
            var spinnerTag = new DivTag();
            spinnerTag.Append(new HtmlTag("i").AddClasses("fa", "fa-spinner", "fa-pulse", "fa-fw"));

            var contentLoadingArea = new DivTag().Data("source-url", url).AddClass("load-action-async");
            if (minHeight > 0)
            {
                //adding a minimum height is optional, but can help prevent the page scrollbar from jumping around while content loads
                contentLoadingArea.Attr("style", $"min-height: {minHeight}px"); 
            }

            contentLoadingArea.Append(placeholderTag);
            contentLoadingArea.Append(spinnerTag);
            return contentLoadingArea;
        }

        public static HtmlTag AjaxPostButton<T>(this HtmlHelper<T> helper, string actionName, string controllerName, string buttonText)
        {
            var url = new UrlHelper(helper.ViewContext.RequestContext).Action(actionName, controllerName);

            var ajaxPostLink = new HtmlTag("a", tag =>
            {
                tag.AddClasses("btn", "btn-primary", "cta", "ajax-button");
                tag.Attr("href", url);
                tag.Text(buttonText);
            });

            return ajaxPostLink;
        }

        public static HtmlString ApplicationVersion(this HtmlHelper helper)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var informationVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            return !string.IsNullOrEmpty(informationVersion) ? new HtmlString($"<span>{informationVersion}</span>") : new HtmlString("");
        }

        public static HtmlTag CheckBoxSquare<T>(this HtmlHelper<T> helper, bool expression, string action) where T : class
        {
            var label = new HtmlTag("label");
            var input = new HtmlTag("input").Attr("type", "checkbox").Attr("disabled","disabled").Attr("checked", true).AddClasses("hide", $"{action}-checkbox");
            const string icon = "<i class='fa fa-fw fa-check-square'></i>";
            if (expression)
                label.Append(input).AppendHtml(icon);
            return label;
        }
    }
}