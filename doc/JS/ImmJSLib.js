/*
 * Copyright 2007 - 2009 Marek St�j
 * 
 * This file is part of ImmDoc .NET.
 *
 * ImmDoc .NET is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * ImmDoc .NET is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ImmDoc .NET; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

/// <summary>
/// Creates new object containing mouse coordinates which are extracted from
/// the supplied JavaScript mouse event.
/// </summary>
/// <param name="e">JavaScript mouse event.</param>
/// <returns>New object containing mouse coordinates..</returns>
function MouseEventArgs(e)
{
    if (e.pageX)
    {
        this.x = e.pageX;
    }
    else if (e.clientX)
    {
        this.x = e.clientX;
    }

    if (e.pageY)
    {
        this.y = e.pageY;
    }
    else if (e.clientY)
    {
        this.y = e.clientY;
    }
}


function GetStyleValue(obj, propertyName)
{
    if (!obj || !propertyName)
    {
        alert("Error: At GetStyleValue(): Parameters 'obj' and 'parameterName' can't be null.");
        
        return null;
    }

    var result = null;

    if (obj.currentStyle)
    {
        result = obj.currentStyle[propertyName];
    }
    else if (window.getComputedStyle)
    {
        try
        {
            var objStyle = window.getComputedStyle(obj, "")
        
            result = objStyle.getPropertyValue(propertyName);
        }
        catch (exc)
        {
            alert("Error: At GetStyleValue(): Couldn't access style information for the specified object.");
        }
    }
    else
    {
        alert("Error: At GetStyleValue(): Couldn't access style information for the specified object.");
        
        return null;
    }
    
    if (!result && propertyName.indexOf('-') != -1)
    {
        var modifiedPropertyName = "";
        
        for (var i = 0; i < propertyName.length; i++)
        {
            var ch = propertyName.charAt(i);
            
            if (ch == '-')
            {
                if (i + 1 < propertyName.length)
                {
                    var nextCh = propertyName.charAt(i + 1);
                    
                    if (nextCh != '-')
                    {
                        modifiedPropertyName += nextCh.toUpperCase();
                        i++;
                    }
                }
            }
            else
            {
                modifiedPropertyName += ch;
            }
        }
        
        return GetStyleValue(obj, modifiedPropertyName);
    }
    
    return result;
}
