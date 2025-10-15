#if STREAM_TESTS_ENABLED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StreamVideo.Libs.PackageXmlInstaller;
using UnityEditor;
using UnityEngine;

namespace StreamVideo.Tests.Editor
{
    public class PackageXmlInstallerTests
    {
        [Test]
        public void All_package_xml_installer_inheritors_should_have_valid_link_xml_asset()
        {
            var inheritorTypes = FindAllPackageXmlInstallerBaseInheritors();

            Assert.IsNotEmpty(inheritorTypes, "No inheritors of PackageXmlInstallerBase found");

            var failedInstances = new List<string>();
            var assetGuidToInstallerType = new Dictionary<string, string>();

            foreach (var type in inheritorTypes)
            {
                Debug.Log($"Testing inheritor: {type.Name}");

                try
                {
                    var instance = Activator.CreateInstance(type);

                    var linkXmlGuid = GetLinkXmlGuid(instance, type);

                    if (string.IsNullOrEmpty(linkXmlGuid))
                    {
                        failedInstances.Add($"{type.Name}: LinkXmlGuid is null or empty");
                        continue;
                    }

                    var assetPath = AssetDatabase.GUIDToAssetPath(linkXmlGuid);

                    if (string.IsNullOrEmpty(assetPath))
                    {
                        failedInstances.Add($"{type.Name}: Asset with GUID '{linkXmlGuid}' not found");
                        continue;
                    }

                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (asset == null)
                    {
                        failedInstances.Add($"{type.Name}: Asset at path '{assetPath}' could not be loaded");
                        continue;
                    }

                    if (assetGuidToInstallerType.TryGetValue(linkXmlGuid, out var value))
                    {
                        failedInstances.Add($"{linkXmlGuid} is used by both {value} and {type.Name}. Each xml installer must have a unique LinkXmlGuid.");
                        continue;
                    }
                    
                    assetGuidToInstallerType.Add(linkXmlGuid, type.Name);

                    Debug.Log($"✓ {type.Name}: LinkXmlGuid '{linkXmlGuid}' -> Asset at '{assetPath}' exists");
                }
                catch (Exception ex)
                {
                    failedInstances.Add($"{type.Name}: Exception occurred - {ex.Message}");
                }
            }

            if (failedInstances.Any())
            {
                var errorMessage = "The following PackageXmlInstallerBase inheritors have issues:\n" +
                                   string.Join("\n", failedInstances);
                Assert.Fail(errorMessage);
            }

            Debug.Log($"✓ All {inheritorTypes.Count} PackageXmlInstallerBase inheritors have valid LinkXmlGuid assets");
        }


        [Test]
        public void Package_xml_installer_base_should_exist()
        {
            var baseType = typeof(PackageXmlInstallerBase);
            Assert.IsNotNull(baseType, nameof(PackageXmlInstallerBase) + " type should exist");
        }

        [Test]
        public void All_package_xml_installer_inheritors_should_be_instantiable()
        {
            var inheritorTypes = FindAllPackageXmlInstallerBaseInheritors();
            var nonInstantiableTypes = new List<string>();

            foreach (var type in inheritorTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(type);
                    Assert.IsNotNull(instance, $"{type.Name} should be instantiable");
                }
                catch (Exception ex)
                {
                    nonInstantiableTypes.Add($"{type.Name}: {ex.Message}");
                }
            }

            if (nonInstantiableTypes.Any())
            {
                var errorMessage = "The following types could not be instantiated:\n" +
                                   string.Join("\n", nonInstantiableTypes);
                Assert.Fail(errorMessage);
            }
        }

        private List<Type> FindAllPackageXmlInstallerBaseInheritors()
        {
            var baseType = typeof(PackageXmlInstallerBase);
            var inheritors = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t != baseType &&
                                    baseType.IsAssignableFrom(t) &&
                                    !t.IsAbstract &&
                                    !t.IsInterface)
                        .ToList();

                    inheritors.AddRange(types);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Some assemblies might not load properly, log warning but continue
                    Debug.LogWarning($"Could not load types from assembly {assembly.FullName}: {ex.Message}");
                }
            }

            return inheritors;
        }

        private string GetLinkXmlGuid(object instance, Type type)
        {
            var property = type.GetProperty(nameof(PackageXmlInstallerBase.LinkXmlGuid),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (property != null)
            {
                return property.GetValue(instance)?.ToString();
            }

            var field = type.GetField(nameof(PackageXmlInstallerBase.LinkXmlGuid),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field != null)
            {
                return field.GetValue(instance)?.ToString();
            }

            var currentType = type.BaseType;
            while (currentType != null && currentType != typeof(object))
            {
                property = currentType.GetProperty(nameof(PackageXmlInstallerBase.LinkXmlGuid),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (property != null)
                {
                    return property.GetValue(instance)?.ToString();
                }

                field = currentType.GetField(nameof(PackageXmlInstallerBase.LinkXmlGuid),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null)
                {
                    return field.GetValue(instance)?.ToString();
                }

                currentType = currentType.BaseType;
            }

            return null;
        }
    }
}
#endif