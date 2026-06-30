using System.Collections.Generic;

namespace Core.Model
{
    public static class CharacterRegistry
    {
        /// <summary>
        /// Global list of all active character models, used for querying proximity/adjacent targets.
        /// </summary>
        public static List<ICharacter> AllCharacters = new List<ICharacter>();
    }
}
