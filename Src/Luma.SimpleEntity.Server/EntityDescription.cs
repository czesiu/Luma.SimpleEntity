using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Luma.SimpleEntity.Helpers;
using RequiredAttribute = System.ComponentModel.DataAnnotations.RequiredAttribute;

namespace Luma.SimpleEntity.Server
{
    /// <summary>
    /// This class provides a metadata description of entity types.
    /// </summary>
    public sealed class EntityDescription
    {
        private static readonly ConcurrentDictionary<Type, HashSet<Type>> TypeDescriptionProviderMap = new ConcurrentDictionary<Type, HashSet<Type>>();
        private readonly Dictionary<Type, List<PropertyDescriptor>> _compositionMap = new Dictionary<Type, List<PropertyDescriptor>>();
        private readonly HashSet<Type> _entityTypes = new HashSet<Type>();
        private Dictionary<Type, HashSet<Type>> _entityKnownTypes;
        private bool _isInitializing;
        private bool _isInitialized;
        private AttributeCollection _attributes;

        /// <summary>
        /// Gets the cache associating entity types with the types
        /// identified with <see cref="KnownTypeAttribute"/>
        /// </summary>
        /// <value>
        /// The result is a dictionary, keyed by entity type and containing
        /// the set of other entity types that were declared via a
        /// <see cref="KnownTypeAttribute"/>.  This set contains only entity
        /// types -- extraneous other known types are omitted.  This set also
        /// contains the full closure of known types for every entity type
        /// by rolling up derived type's known types onto their base.
        /// This cache is lazily loaded but stable.  This means that we capture
        /// the list of known types once and are not affected by the semantics
        /// of <see cref="KnownTypeAttribute"/> that permit a runtime method
        /// to return potentially different known types.
        /// </value>
        public Dictionary<Type, HashSet<Type>> EntityKnownTypes
        {
            get
            {
                if (_entityKnownTypes == null)
                {
                    _entityKnownTypes = ComputeEntityKnownTypes();
                }

                return _entityKnownTypes;
            }
        }

        /// <summary>
        /// Gets the entity types
        /// </summary>
        public IEnumerable<Type> EntityTypes
        {
            get
            {
                EnsureInitialized();

                foreach (Type entityType in _entityTypes)
                {
                    yield return entityType;
                }
            }
        }

        /// <summary>
        /// Returns the root type for the given entity type.
        /// </summary>
        /// <remarks>
        /// The root type is the least derived entity type in the entity type hierarchy.
        /// </remarks>
        /// <param name="entityType">The entity type whose root is required.</param>
        /// <returns>The type of the root or <c>null</c> if the given <paramref name="entityType"/>
        /// has no base types.</returns>
        public Type GetRootEntityType(Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException("entityType");
            }
            Type rootType = null;
            while (entityType != null)
            {
                if (EntityTypes.Contains(entityType))
                {
                    rootType = entityType;
                }
                entityType = entityType.BaseType;
            }
            return rootType;
        }

        /// <summary>
        /// Gets the base type of the given entity type.
        /// </summary>
        /// <remarks>
        /// The base type is the closest base type of
        /// the given entity type. The entity hierarchy
        /// may contain types that are not exposed, and this
        /// method skips those.
        /// </remarks>
        /// <param name="entityType">The entity type whose base type is required.</param>
        /// <returns>The base type or <c>null</c> if the given
        /// <paramref name="entityType"/> had no visible base types.</returns>
        public Type GetEntityBaseType(Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException("entityType");
            }

            Type baseType = entityType.BaseType;
            while (baseType != null)
            {
                if (EntityTypes.Contains(baseType))
                {
                    break;
                }
                baseType = baseType.BaseType;
            }
            return baseType;
        }

        /// <summary>
        /// Validate and initialize the description. Initialize should be called before the description
        /// is used. Only descriptions that have been created manually need to be initialized. Descriptions
        /// returned by the framework are already initialized.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }
            _isInitializing = true;

            // only AFTER all entities have been identified and have had their
            // TDPs registered can we search for complex types
            DiscoverOtherEntityTypes();

            // After the entity hierarchy is complete, we have sufficient
            // context to detect subclasses of composition children and
            // to locate inherited parent associations.
            FixupCompositionMap();

            // only after all custom type description providers have been registered can we
            // perform validations on entities and complex types, since the PropertyDescriptors
            // must include all extension metadata
            ValidateEntityTypes();

            _isInitialized = true;
            _isInitializing = false;
        }

        /// <summary>
        /// Enumerates all entity types and operations searching for complex types and
        /// adds them to our complex types collection.
        /// <remarks>
        /// Nowhere on this codepath can MetaTypes be accessed for complex types, since we
        /// require ALL complex types to have their TDPs registered before hand.
        /// </remarks>
        /// </summary>
        private void DiscoverOtherEntityTypes()
        {
            // discover and add other entity types that are entity properties
            foreach (var entityType in EntityTypes.ToArray())
            {
                foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(entityType))
                {
                    if (pd.Attributes[typeof(ExcludeAttribute)] != null)
                    {
                        continue;
                    }

                    var memberType = TypeUtility.GetElementType(pd.PropertyType);

                    if (IsValidEntityType(memberType))
                    {
                        AddEntityType(memberType);
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the specified type is a known entity type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>true if the type is a known entity type; false otherwise.</returns>
        internal bool IsKnownEntityType(Type type)
        {
            return _entityTypes.Contains(type);
        }

        /// <summary>
        /// Call this method to ensure that the description is initialized.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isInitialized && !_isInitializing)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Resource.EntityDescription_Uninitialized"));
            }
        }

        /// <summary>
        /// Register all required custom type descriptors for the specified type.
        /// </summary>
        /// <param name="type">The type to register.</param>
        private void RegisterCustomTypeDescriptors(Type type)
        {
            // if this type has a metadata class defined, add a 'buddy class' type descriptor. 
            if (type.GetCustomAttributes(typeof(MetadataTypeAttribute), true).Length != 0)
            {
                RegisterCustomTypeDescriptor(new AssociatedMetadataTypeTypeDescriptionProvider(type), type);
            }
        }

        // The JITer enforces CAS. By creating a separate method we can avoid getting SecurityExceptions 
        // when we weren't going to really call TypeDescriptor.AddProvider.
        internal static void RegisterCustomTypeDescriptor(TypeDescriptionProvider tdp, Type type)
        {
            // Check if we already registered provider with the specified type.
            HashSet<Type> existingProviders = TypeDescriptionProviderMap.GetOrAdd(type, t =>
            {
                return new HashSet<Type>();
            });

            if (!existingProviders.Contains(tdp.GetType()))
            {
                TypeDescriptor.AddProviderTransparent(tdp, type);
                existingProviders.Add(tdp.GetType());
            }
        }

        /// <summary>
        /// Determines whether a given type may be used as an entity type.
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <param name="errorMessage">If this method returns <c>false</c>, the error message</param>
        /// <returns><c>true</c> if the type can legally be used as an entity</returns>
        private static bool IsValidEntityType(Type type, out string errorMessage)
        {
            errorMessage = null;

            if (!type.IsVisible)
            {
                errorMessage = Resource.EntityTypes_Must_Be_Public;
            }
            else if (TypeUtility.IsNullableType(type))
            {
                // why is this check here? can't we just assert that an entity type
                // is not a value type?
                errorMessage = Resource.EntityTypes_Cannot_Be_Nullable;
            }
            else if (TypeUtility.IsPredefinedType(type))
            {
                errorMessage = Resource.EntityTypes_Cannot_Be_Primitives;
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                errorMessage = Resource.EntityTypes_Cannot_Be_Collections;
            }
            else if (!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) == null)
            {
                // Lack of ctor counts only if not abstract.
                errorMessage = Resource.EntityTypes_Must_Have_Default_Constructor;
            }

            return (errorMessage == null);
        }

        private static bool IsValidEntityType(Type type)
        {
            string errorMessage;
            return IsValidEntityType(type, out errorMessage);
        }

        public void TryAddEntityType(Type type)
        {
            if (!IsKnownEntityType(type))
            {
                RegisterCustomTypeDescriptors(type);

                var typeAttributes = TypeDescriptor.GetAttributes(type);
                if (typeAttributes[typeof(EntityAttribute)] != null)
                {
                    AddEntityType(type);
                }
                else if (typeAttributes[typeof(ExcludeAttribute)] == null)
                {
                    var properties = TypeDescriptor.GetProperties(type).Cast<PropertyDescriptor>();

                    if (properties.Any(c => c.Attributes[typeof(KeyAttribute)] != null) && IsValidEntityType(type))
                    {
                        AddEntityType(type);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively add the specified entity and all entities in its Include graph
        /// to our list of all entities, and register an associated metadata type descriptor
        /// for each.
        /// </summary>
        /// <param name="entityType">type of entity to add</param>
        public void AddEntityType(Type entityType)
        {
            if (IsKnownEntityType(entityType))
            {
                // we've already visited this type
                return;
            }

            // Ensure this type can really be used as an entity type
            string errorMessage;
            if (!IsValidEntityType(entityType, out errorMessage))
            {
                Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, Resource.Invalid_Entity_Type, entityType.Name, errorMessage));
                return;
            }

            _entityTypes.Add(entityType);

            // Any new entity invalidates our cached entity hierarchies
            _entityKnownTypes = null;

            RegisterCustomTypeDescriptors(entityType);

            // Recursively add any derived entity types specified by [KnownType]
            // attributes
            var knownTypes = KnownTypeUtilities.ImportKnownTypes(entityType, true);
            foreach (var t in knownTypes)
            {
                if (entityType.IsAssignableFrom(t))
                {
                    AddEntityType(t);
                }
            }
        }

        /// <summary>
        /// Validates that the given <paramref name="entityType"/> does not contain
        /// any properties that violate our rules for polymorphism.
        /// </summary>
        /// <remarks>
        /// The only rule currently enforced is that no property can use "new" to
        /// override an existing entity property.
        /// </remarks>
        /// <param name="entityType">The entity type to validate.</param>
        private static void ValidatePropertyPolymorphism(Type entityType)
        {
            Debug.Assert(entityType != null, "entityType is required");

            // We consider only actual properties, not TDP properties, because
            // these are the only ones that can have actual runtime methods
            // that are polymorphic.  We ask only for public instance properties.
            foreach (PropertyInfo propertyInfo in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                //skip indexer properties.
                if (propertyInfo.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                // Either the get or the set will suffice
                MethodInfo methodInfo = propertyInfo.GetGetMethod() ?? propertyInfo.GetSetMethod();

                // Detecting the "new" keyword requires a check whether this method is
                // hiding a method with the same signature in a derived type.  IsHideBySig does not do this.
                // A "new" constitutes an illegal use of polymorphism and throws InvalidOperationException.
                // A "new" appears as a non-virtual method that is declared concretely further up the hierarchy.
                if (methodInfo.DeclaringType.BaseType != null && !methodInfo.IsVirtual)
                {
                    Type[] parameterTypes = methodInfo.GetParameters().Select<ParameterInfo, Type>(p => p.ParameterType).ToArray();
                    MethodInfo baseMethod = methodInfo.DeclaringType.BaseType.GetMethod(methodInfo.Name, parameterTypes);
                    if (baseMethod != null && !baseMethod.IsAbstract)
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                                                            Resource.Entity_Property_Redefined,
                                                            entityType,
                                                            propertyInfo.Name,
                                                            baseMethod.DeclaringType));
                    }
                }
            }
        }

        /// <summary>
        /// Validate all entity types exposed by this provider.
        /// </summary>
        private void ValidateEntityTypes()
        {
            foreach (Type entityType in _entityTypes)
            {
                Type rootType = GetRootEntityType(entityType);
                bool isRootType = entityType == rootType;
                string firstKeyProperty = null;
                PropertyDescriptor versionMember = null;

                PropertyDescriptorCollection pds = TypeDescriptor.GetProperties(entityType);
                foreach (PropertyDescriptor pd in pds)
                {
                    // During first pass, just notice whether any property is marked [Key]
                    if (firstKeyProperty == null)
                    {
                        if (pd.Attributes[typeof(ExcludeAttribute)] == null)
                        {
                            var hasKey = (pd.Attributes[typeof(KeyAttribute)] != null);

                            // The presence of a [Key] property matters for the root type
                            // regardless if it is actually declared there or on some hidden
                            // base type (ala UserBase).  But for derived entities, it matters
                            // only if they explicitly declare it.
                            if (hasKey && (isRootType || pd.ComponentType == entityType))
                            {
                                firstKeyProperty = pd.Name;
                            }
                        }
                    }

                    // Verify that multiple version members are not defined
                    if (pd.Attributes[typeof(TimestampAttribute)] != null && pd.Attributes[typeof(ConcurrencyCheckAttribute)] != null)
                    {
                        if (versionMember != null)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.EntityDescription_MultipleVersionMembers, entityType));
                        }
                        versionMember = pd;
                    }
                }

                // validate associations defined in this entity type
                ValidateEntityAssociations(entityType, pds);

                // Validate derived entity types don't attempt illegal polymorphism
                ValidatePropertyPolymorphism(entityType);

                // Validate that:
                //  - The root type must declare a [KnownType] for every derived type
                //  - Every abstract entity type must specify a [KnownType] for at least one concrete derived type
                ValidateKnownTypeAttribute(entityType, /*mustSpecifyAll*/ isRootType);

                // Validate that if the type derives from one that has [DataContract] attribute,
                // then it has a [DataContract] attribute as well.
                ValidateDataContractAttribute(entityType);
            }
        }

        /// <summary>
        /// Fixes the map of composed types so that derived composition types
        /// inherit the parent associations from their base type.
        /// </summary>
        /// <remarks>
        /// This method assumes the composition map has been constructed
        /// to contain the parent associations for each composed type without
        /// regard to inheritance.  Hence, composition subclasses will not have
        /// entries in the map unless they have additional parent associations.
        /// Composition subclasses that have no entry will gain a new entry
        /// that is the accumulation of all their base class's associations.
        /// Composition subclasses that have their own parent associations will
        /// combine those with the accumulated base class's associations.
        /// </remarks>
        private void FixupCompositionMap()
        {
            // Strategy: to accommodate possible splits in the hierarchy and
            // the indeterminate order in which the entities are in our cache,
            // we maintain a hash to guarantee we fix each entry only once.
            var fixedEntities = new HashSet<Type>();
            foreach (Type entityType in this.EntityTypes)
            {
                FixParentAssociationsWalk(entityType, fixedEntities);
            }
        }

        /// <summary>
        /// Helper method to ensure this <paramref name="entityType"/>'s entry in
        /// the composition map combines its explicit parent associations together
        /// with those from all its base type's associations.
        /// </summary>
        /// <remarks>
        /// This algorithm repairs the composition map in-place and reentrantly
        /// fixes the entity's base class entries first.  The map will not be replaced,
        /// but will be updated in-place.  Existing lists in the map will be extended
        /// but not replaced.  Holes in the map will be filled in by sharing the base
        /// class's entry rather than cloning.
        /// </remarks>
        /// <param name="entityType">The entity type to repair.  It may or may not be a composed type.</param>
        /// <param name="fixedEntities">Hash of already repaired entity types.  Used to avoid duplicate fixups.</param>
        /// <returns>The collection of parent associations.</returns>
        private List<PropertyDescriptor> FixParentAssociationsWalk(Type entityType, HashSet<Type> fixedEntities)
        {
            List<PropertyDescriptor> parentAssociations = null;

            // If we have already visited this entity type, its composition map
            // entry is accurate and can be used as is.
            this._compositionMap.TryGetValue(entityType, out parentAssociations);
            if (fixedEntities.Contains(entityType))
            {
                return parentAssociations;
            }

            fixedEntities.Add(entityType);

            // Get the base class's associations.  This will re-entrantly walk back
            // the hierarchy and repair the composition map as it goes.
            Type baseType = this.GetEntityBaseType(entityType);
            List<PropertyDescriptor> inheritedParentAssociations = (baseType == null)
                                                                            ? null
                                                                            : this.FixParentAssociationsWalk(baseType, fixedEntities);

            // If there are no base class associations to inherit, then the map
            // is already accurate for the current entry.  But if we have base
            // class associations, we need either to merge or to share them.
            if (inheritedParentAssociations != null)
            {
                if (parentAssociations == null)
                {
                    // No current associations -- simply share the base class's list
                    parentAssociations = inheritedParentAssociations;
                    _compositionMap[entityType] = parentAssociations;
                }
                else
                {
                    // Associations for both base and current -- merge them into existing list
                    parentAssociations.AddRange(inheritedParentAssociations);
                }
            }
            return parentAssociations;
        }

        /// <summary>
        /// Validates that the specified root entity type
        /// has a <see cref="KnownTypeAttribute"/> for each of its
        /// derived types.
        /// </summary>
        /// <param name="entityType">The entity type to check.</param>
        /// <param name="mustSpecifyAll">If <c>true</c> this method validates that this entity declares all its derived types via <see cref="KnownTypeAttribute"/>.</param>
        private void ValidateKnownTypeAttribute(Type entityType, bool mustSpecifyAll)
        {
            IEnumerable<Type> knownTypes = this.EntityKnownTypes[entityType];
            IEnumerable<Type> derivedTypes = this.GetEntityDerivedTypes(entityType);
            bool hasConcreteDerivedType = false;

            foreach (Type derivedType in derivedTypes)
            {
                hasConcreteDerivedType |= !derivedType.IsAbstract;

                if (mustSpecifyAll && !knownTypes.Contains(derivedType))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Resource.KnownTypeAttributeRequired", derivedType.Name, entityType.Name));
                }
            }

            // Any abstract entity type is required to use [KnownType] to specify
            // at least one concrete subclass
            if (entityType.IsAbstract && !hasConcreteDerivedType)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "The entity type '{0}' is abstract and must use a KnownTypeAttribute to specify at least one non-abstract derived type.", entityType.Name));
            }
        }

        /// <summary>
        /// Validates that if the specified type derives from a type with 
        /// <see cref="DataContractAttribute"/>, then it has a 
        /// <see cref="DataContractAttribute"/> as well.
        /// </summary>
        /// <param name="entityType">The entity type to check.</param>
        private static void ValidateDataContractAttribute(Type entityType)
        {
            if (entityType.Attributes()[typeof(DataContractAttribute)] != null)
            {
                return;
            }

            Type baseType = entityType.BaseType;
            while (baseType != null)
            {
                if (baseType.Attributes()[typeof(DataContractAttribute)] != null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.EntityDescription_DataContractAttributeRequired, entityType.Name, baseType.Name));
                }
                baseType = baseType.BaseType;
            }
        }

        /// <summary>
        /// This method validates the association attributes for the specified entity type
        /// </summary>
        /// <param name="entityType">Type of entity to validate its association attributes for</param>
        /// <param name="entityProperties">collection of entity property descriptors</param>
        private void ValidateEntityAssociations(Type entityType, PropertyDescriptorCollection entityProperties)
        {
            foreach (PropertyDescriptor pd in entityProperties)
            {
                // validate the association attribute (if any)
                var assocAttrib = pd.Attributes[typeof(AssociationAttribute)] as AssociationAttribute;
                if (assocAttrib == null)
                {
                    continue;
                }

                string assocName = assocAttrib.Name;
                if (string.IsNullOrEmpty(assocName))
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Association defined on property '{0}' of entity '{1}' is invalid: Name cannot be null or empty.", pd.Name, entityType));
                }
                if (string.IsNullOrEmpty(assocAttrib.ThisKey))
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Association named '{0}' defined on entity type '{1}' is invalid: {2} cannot be null or empty.", assocName, entityType, "ThisKey"));
                }
                if (string.IsNullOrEmpty(assocAttrib.OtherKey))
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Resource.InvalidAssociation_StringCannotBeNullOrEmpty", assocName, entityType, "OtherKey"));
                }

                // The number of keys in 'this' and 'other' must be the same
                if (assocAttrib.ThisKeyMembers.Count() != assocAttrib.OtherKeyMembers.Count())
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Resource.InvalidAssociation_Key_Count_Mismatch", assocName, entityType, assocAttrib.ThisKey, assocAttrib.OtherKey));
                }

                // check that all ThisKey members exist on this entity type
                foreach (string thisKey in assocAttrib.ThisKeyMembers)
                {
                    if (entityProperties[thisKey] == null)
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Resource.InvalidAssociation_ThisKeyNotFound", assocName, entityType, thisKey));
                    }
                }

                // Verify that the association name is unique. In inheritance scenarios, self-referencing associations
                // on the base type should be inheritable by the derived types.
                Type otherEntityType = TypeUtility.GetElementType(pd.PropertyType);
                int otherMemberCount = entityProperties.Cast<PropertyDescriptor>().Count(p => p.Name != pd.Name && p.Attributes.OfType<AssociationAttribute>().Any(a => a.Name == assocAttrib.Name));
                bool isSelfReference = otherEntityType.IsAssignableFrom(entityType);
                if ((!isSelfReference && otherMemberCount > 0) || (isSelfReference && otherMemberCount > 1))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Resource.InvalidAssociation_NonUniqueAssociationName", assocName, entityType));
                }

                // Verify that the type of FK associations return singletons.
                if (assocAttrib.IsForeignKey && (otherEntityType != pd.PropertyType))
                {
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.InvalidAssociation_FKNotSingleton,
                        assocName, entityType));
                }

                // Associations are not allowed to be marked as [Required], because we don't guarantee 
                // that we set the association on the server. In many cases it's possible that we simply 
                // associate entities based on FKs.
                if (pd.Attributes[typeof(RequiredAttribute)] != null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Resource.Entity_RequiredAssociationNotAllowed", entityType, pd.Name));
                }

                // if the other entity is also exposed by the service, perform additional validation
                if (_entityTypes.Contains(otherEntityType))
                {
                    PropertyDescriptorCollection otherEntityProperties = TypeDescriptor.GetProperties(otherEntityType);
                    PropertyDescriptor otherMember = otherEntityProperties.Cast<PropertyDescriptor>().FirstOrDefault(p => p.Name != pd.Name && p.Attributes.OfType<AssociationAttribute>().Any(a => a.Name == assocName));
                    if (otherMember != null)
                    {
                        // Bi-directional association
                        // make sure IsForeignKey is set to true on one and only one side of the association
                        AssociationAttribute otherAssocAttrib = (AssociationAttribute)otherMember.Attributes[typeof(AssociationAttribute)];
                        if (otherAssocAttrib != null &&
                            !((assocAttrib.IsForeignKey != otherAssocAttrib.IsForeignKey)
                             && (assocAttrib.IsForeignKey || otherAssocAttrib.IsForeignKey)))
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_IsFKInvalid, assocName, entityType));
                        }

                        Type otherMemberEntityType = TypeUtility.GetElementType(otherMember.PropertyType);

                        // Verify that the type of the corresponding association points back to this entity
                        // The type of the corresponding association can be one of the parents of the entity, but it cannot be one of its children.
                        if (!otherMemberEntityType.IsAssignableFrom(entityType))
                        {
                            throw new InvalidOperationException(string.Format(
                                CultureInfo.CurrentCulture,
                                Resource.InvalidAssociation_TypesDoNotAlign,
                                assocName, entityType, otherEntityType));
                        }
                    }

                    // check that the OtherKey members exist on the other entity type
                    foreach (string otherKey in assocAttrib.OtherKeyMembers)
                    {
                        if (otherEntityProperties[otherKey] == null)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_OtherKeyNotFound, assocName, entityType, otherKey, otherEntityType));
                        }
                    }
                }
                else
                {
                    // Disallow attempts to place [Association] on simple types
                    if (TypeUtility.IsPredefinedType(otherEntityType))
                    {
                        // Association attributes cannot be attached to properties whose types are not entities
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.Association_Not_Entity_Type, pd.Name, entityType.Name, otherEntityType.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Computes the closure of known types for all the known entities.
        /// See <see cref="EntityKnownTypes"/>
        /// </summary>
        /// <returns>A dictionary, keyed by entity type and containing all the
        /// declared known types for it, including the transitive closure.
        /// </returns>
        private Dictionary<Type, HashSet<Type>> ComputeEntityKnownTypes()
        {
            Dictionary<Type, HashSet<Type>> closure = new Dictionary<Type, HashSet<Type>>();

            // Gather all the explicit known types from attributes.
            // Because we ask to inherit [KnownType], we will collect the full closure
            foreach (Type entityType in this.EntityTypes)
            {
                // Get all [KnownType]'s and subselect only those that actually derive from this entity
                IEnumerable<Type> knownTypes = KnownTypeUtilities.ImportKnownTypes(entityType, /* inherit */ true).Where(t => entityType.IsAssignableFrom(t));
                closure[entityType] = new HashSet<Type>(knownTypes);
            }

            // 2nd pass -- add all the derived types' known types back to their base so we have the closure
            foreach (Type entityType in this.EntityTypes)
            {
                IEnumerable<Type> knownTypes = closure[entityType];
                for (Type baseType = this.GetEntityBaseType(entityType);
                     baseType != null;
                     baseType = this.GetEntityBaseType(baseType))
                {
                    HashSet<Type> hash = closure[baseType];
                    foreach (Type knownType in knownTypes)
                    {
                        hash.Add(knownType);
                    }
                }
            }
            return closure;
        }

        /// <summary>
        /// Returns the collection of all entity types derived from <paramref name="entityType"/>
        /// </summary>
        /// <param name="entityType">The entity type whose derived types are needed.</param>
        /// <returns>The collection of derived types.  It may be empty.</returns>
        public IEnumerable<Type> GetEntityDerivedTypes(Type entityType)
        {
            System.Diagnostics.Debug.Assert(entityType != null, "GetEntityDerivedTypes(null) not allowed");
            return EntityTypes.Where(et => et != entityType && entityType.IsAssignableFrom(et));
        }

    }
}
