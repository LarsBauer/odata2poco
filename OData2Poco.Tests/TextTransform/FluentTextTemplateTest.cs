﻿using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using OData2Poco.TextTransform;

namespace OData2Poco.Tests.TextTransform
{
    [Category("FluentTextTemplate")]
    class FluentTextTemplateTest
    {
         string _nl=Environment.NewLine;
        [Test]
        public void WriteList_test()
        {
            var ft = new FluentTextTemplate();
            var text = new[] { "public", "abstract", "class", "MyClass" }.ToList();
            string result = ft.WriteList(text);
            var expected = "public abstract class MyClass";
            Assert.That(result, Is.EqualTo(expected));
        }
        [Test]
        public void WriteList_string_comma_separator_test()
        {
            FluentTextTemplate ft = new FluentTextTemplate();
            var text = "public,abstract, class, MyClass";
            string result = ft.WriteList(text);
            var expected = "public abstract class MyClass";
            Assert.That(result, Is.EqualTo(expected));
        }
        [Test]
        public void WriteList_string_space_separator_test()
        {
            FluentTextTemplate ft = new FluentTextTemplate();
            var text = "public abstract class    MyClass";
            string result = ft.WriteList(text);
            var expected = "public abstract class MyClass";
            Assert.That(result, Is.EqualTo(expected));
        }
        [Test]
        [TestCase(false, "private")]
        [TestCase(true, "public")]
        public void Write_if_true_false_test(bool isTrue, string expected)
        {
            FluentTextTemplate ft = new FluentTextTemplate();
            string result = ft.WriteIf(isTrue, "public", "private");
            Assert.That(result, Is.EqualTo(expected));
        }
        [Test]
        [TestCase(false, "public")]
        [TestCase(true, "public abstract")]
        public void Write_if_true_test(bool isTrue, string expected)
        {
            FluentTextTemplate ft = new FluentTextTemplate();
            string result = ft.Write("public")
                .WriteIf(isTrue, " abstract");
            Assert.That(result, Is.EqualTo(expected));
        }
        [Test]
        public void Template_with_header_Test()
        {
            FluentTextTemplate ft = new FluentTextTemplate { Header = "this is header" };
            string result = ft.Write("hellow world");
            var expected = $"this is header{_nl}hellow world";
            Assert.That(result, Is.EqualTo(expected));
        }
        [Test]
        public void Template_with_header_footer_Test()
        {
            FluentTextTemplate ft = new FluentTextTemplate
            {
                Header = "this is a header",
                Footer = "that is a footer"
            };
            string result = ft.Write("hellow world");
            var expected = $"this is a header{_nl}hellow world{_nl}that is a footer{_nl}";
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
