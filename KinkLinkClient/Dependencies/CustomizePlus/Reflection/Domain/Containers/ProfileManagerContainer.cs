using System.Reflection;

namespace KinkLinkClient.Dependencies.CustomizePlus.Reflection.Domain.Containers;

public record ProfileManagerContainer(MethodInfo AddCharacter, MethodInfo AddTemplate, MethodInfo CloneMethod, MethodInfo Create, MethodInfo Delete, MethodInfo SetEnabled, MethodInfo SetPriority);
