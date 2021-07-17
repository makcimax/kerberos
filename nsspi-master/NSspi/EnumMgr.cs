using System;
using System.Reflection;

namespace NSspi
{
    /// <summary>
    /// Помечает член перечисления строкой, к которой можно получить программный доступ. 
    /// </summary>
    [AttributeUsage( AttributeTargets.Field )]
    public class EnumStringAttribute : Attribute
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref = "EnumStringAttribute" />. 
        /// </summary>
        /// <param name="text">Строка, которую нужно связать с членом перечисления. </param>
        public EnumStringAttribute( string text )
        {
            this.Text = text;
        }

        /// <summary>
        /// Получает строку, связанную с членом перечисления. 
        /// </summary>
        public string Text { get; private set; }
    }

    /// <summary>
    /// Преобразует между членами перечисления и строками, связанными с членами через
    /// тип <see cref = "EnumStringAttribute" />. 
    /// </summary>
    public class EnumMgr
    {
        /// <summary>
        /// Получает текст, связанный с заданным членом перечисления, через <см. Cref = "EnumStringAttribute" />. 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToText( Enum value )
        {
            FieldInfo field = value.GetType().GetField( value.ToString() );

            EnumStringAttribute[] attribs = (EnumStringAttribute[])field.GetCustomAttributes( typeof( EnumStringAttribute ), false );

            if( attribs == null || attribs.Length == 0 )
            {
                return null;
            }
            else
            {
                return attribs[0].Text;
            }
        }

        /// <summary>
        /// Возвращает член перечисления, помеченный заданным текстом с использованием типа <see cref = "EnumStringAttribute" />. 
        /// </summary>
        /// <typeparam name="T">Тип перечисления для проверки. </typeparam>
        /// <param name="text"></param>
        /// <returns></returns>
        public static T FromText<T>( string text )
        {
            FieldInfo[] fields = typeof( T ).GetFields();

            EnumStringAttribute[] attribs;

            foreach( FieldInfo field in fields )
            {
                attribs = (EnumStringAttribute[])field.GetCustomAttributes( typeof( EnumStringAttribute ), false );

                foreach( EnumStringAttribute attrib in attribs )
                {
                    if( attrib.Text == text )
                    {
                        return (T)field.GetValue( null );
                    }
                }
            }

            throw new ArgumentException( "Could not find a matching enumeration value for the text '" + text + "'." );
        }
    }
}