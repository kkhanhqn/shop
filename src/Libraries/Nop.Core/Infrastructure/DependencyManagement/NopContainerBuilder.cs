using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Core.Infrastructure.DependencyManagement
{
    /// <summary>
    /// Used to build an Autofac.IContainer from component registrations
    /// </summary>
    public partial class NopContainerBuilder
    {
        #region Fields

        protected readonly ContainerBuilder _containerBuilder;
        protected readonly List<IRegistrationBuilder<object, ReflectionActivatorData, object>> _registeredTypes;
        protected Dictionary<string, RegisteredCollisionData> _buildCollisions;

        #endregion

        #region Ctor

        public NopContainerBuilder()
        {
            this._containerBuilder = new ContainerBuilder();
            this._registeredTypes = new List<IRegistrationBuilder<object, ReflectionActivatorData, object>>();
            this._buildCollisions = new Dictionary<string, RegisteredCollisionData>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Register an instance as a component
        /// </summary>
        /// <typeparam name="T">The type of the instance</typeparam>
        /// <param name="instance">The instance to register</param>
        /// <returns>Registration builder allowing the registration to be configured</returns>
        public virtual IRegistrationBuilder<T, SimpleActivatorData, SingleRegistrationStyle> RegisterInstance<T>(
            T instance)
            where T : class
        {
            return _containerBuilder.RegisterInstance(instance);
        }

        /// <summary>
        /// Register a component to be created through reflection
        /// </summary>
        /// <typeparam name="TImplementer">The type of the component implementation</typeparam>
        /// <returns>Registration builder allowing the registration to be configured</returns>
        public virtual IRegistrationBuilder<TImplementer, ConcreteReflectionActivatorData, SingleRegistrationStyle>
            RegisterType<TImplementer>()
        {
            var registeredType = _containerBuilder.RegisterType<TImplementer>();
            _registeredTypes.Add(registeredType as IRegistrationBuilder<object, ConcreteReflectionActivatorData, object>);

            return registeredType;
        }

        /// <summary>
        /// Register a component to be created through reflection
        /// </summary>
        /// <param name="implementationType">The type of the component implementation</param>
        /// <returns>Registration builder allowing the registration to be configured</returns>
        public virtual IRegistrationBuilder<object, ConcreteReflectionActivatorData,
            SingleRegistrationStyle> RegisterType(Type implementationType)
        {
            return _containerBuilder.RegisterType(implementationType);
        }

        /// <summary>
        /// Register a delegate as a component
        /// </summary>
        /// <typeparam name="T">The type of the instance</typeparam>
        /// <param name="func">The delegate to register</param>
        /// <returns>Registration builder allowing the registration to be configured</returns>
        public virtual IRegistrationBuilder<T, SimpleActivatorData, SingleRegistrationStyle> Register<T>(
            Func<IComponentContext, T> func)
        {
            return _containerBuilder.Register(func);
        }

        /// <summary>
        /// Register an un-parameterised generic type, e.g. Repository<>. Concrete types will be made as they are requested, e.g. with Resolve<Repository<int>>()
        /// </summary>
        /// <param name="implementer">The open generic implementation type</param>
        /// <returns>Registration builder allowing the registration to be configured</returns>
        public virtual IRegistrationBuilder<object, ReflectionActivatorData,
            DynamicRegistrationStyle> RegisterGeneric(Type implementer)
        {
            var registeredGeneric = _containerBuilder.RegisterGeneric(implementer);
            _registeredTypes.Add(registeredGeneric);

            return registeredGeneric;
        }

        /// <summary>
        /// Add a registration source to the container
        /// </summary>
        /// <param name="registrationSource">The registration source to add</param>
        public virtual void RegisterSource(IRegistrationSource registrationSource)
        {
            _containerBuilder.RegisterSource(registrationSource);
        }

        /// <summary>
        /// Create a new container with the component registrations that have been made
        /// </summary>
        /// <param name="options">Options that influence the way the container is initialised</param>
        /// <returns>A new container with the configured component registrations</returns>
        public virtual IContainer Build(ContainerBuildOptions options = 0)
        {
            return _containerBuilder.Build(options);
        }

        /// <summary>
        /// Populates the Autofac container builder with the set of registered service descriptors and makes System.IServiceProvider and Microsoft.Extensions.DependencyInjection.IServiceScopeFactory available in the container
        /// </summary>
        /// <param name="descriptors">The set of service descriptors to register in the container</param>
        public virtual void Populate(IEnumerable<ServiceDescriptor> descriptors)
        {
            _containerBuilder.Populate(descriptors);
        }

        /// <summary>
        /// Indicates whether there are conflicts in the registration of types
        /// </summary>
        public virtual bool HasRegisteredCollisions
        {
            get
            {
                if (_buildCollisions.Any())
                    return true;

                //we ignore all core libraries
                var filterModuleNames = new List<string>
                {
                    "Nop.Core.dll",
                    "Nop.Data.dll",
                    "Nop.Services.dll",
                    "Nop.Web.dll",
                    "Nop.Web.Framework.dll"
                };

                //getting all registered types, which implement one interface twice or more times
                var registeredTypeCollisions = _registeredTypes
                    .SelectMany(registeredType => registeredType.RegistrationData.Services.Select(x => new { x.Description, RegisteredType = registeredType }))
                    .GroupBy(p => p.Description)
                    .ToDictionary(p => p.Key, p => p.ToList());

                foreach (var key in registeredTypeCollisions.Keys)
                {
                    //getting libraries which have an implement of specific interface
                    var modules = registeredTypeCollisions[key]
                        .Select(p => p.RegisteredType.ActivatorData.ImplementationType.Module.Name)
                        .Where(p=>!filterModuleNames.Contains(p))
                        .ToList();

                    if(modules.Count <= 1)
                        continue;

                    if (!_buildCollisions.ContainsKey(key))
                        _buildCollisions.Add(key, new RegisteredCollisionData
                        {
                            InterfaceName = key,
                            UsedModule = modules.Last()
                        });

                    _buildCollisions[key].ModuleNames.AddRange(modules);
                }

                return _buildCollisions.Any();
            }
        }

        /// <summary>
        /// Gets conflicts in the registration of types
        /// </summary>
        public virtual List<RegisteredCollisionData> GetRegisteredCollisions
        {
            get { return HasRegisteredCollisions ? _buildCollisions.Values.ToList() : new List<RegisteredCollisionData>(); }
        }

        #endregion

        #region Nested classes

        /// <summary>
        /// Conflicts information in the registration of types
        /// </summary>
        public class RegisteredCollisionData
        {
            private string _interfaceName;

            /// <summary>
            /// Interface name
            /// </summary>
            public string InterfaceName
            {
                get { return _interfaceName; }
                internal set { _interfaceName = value.Replace("`1", "<T>"); }
            }

            /// <summary>
            /// Module names
            /// </summary>
            public List<string> ModuleNames { get; } = new List<string>();

            /// <summary>
            /// Used module
            /// </summary>
            public string UsedModule { get; internal set; }

            /// <summary>
            /// Module names as one string separated by comma
            /// </summary>
            public string ModuleNamesString
            {
                get
                {
                    return ModuleNames.Aggregate("", (agregate, current) => agregate + ", " + current).TrimStart(',', ' ');
                }
            }
        }

        #endregion
    }
}