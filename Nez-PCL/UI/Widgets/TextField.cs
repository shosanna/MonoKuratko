﻿using System;
using Nez.BitmapFonts;
using Microsoft.Xna.Framework;
using System.Text;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;


namespace Nez.UI
{
	/// <summary>
	/// A single-line text input field.
	/// 
	/// The preferred height of a text field is the height of the {@link TextFieldStyle#font} and {@link TextFieldStyle#background}.
	/// The preferred width of a text field is 150, a relatively arbitrary size.
	/// 
	/// The text field will copy the currently selected text when ctrl+c is pressed, and paste any text in the clipboard when ctrl+v is
	/// pressed. Clipboard functionality is provided via the {@link Clipboard} interface.
	/// 
	/// The text field allows you to specify an {@link OnscreenKeyboard} for displaying a softkeyboard and piping all key events
	/// generated by the keyboard to the text field. There are two standard implementations, one for the desktop and one for Android.
	/// The desktop keyboard is a stub, as a softkeyboard is not needed on the desktop. The Android {@link OnscreenKeyboard}
	/// implementation will bring up the default IME.
	/// </summary>
	public class TextField : Element, IInputListener, IKeyboardListener
	{
		public event Action<TextField,string> onTextChanged;

		public override float preferredWidth
		{
			get { return _preferredWidth; }
		}

		public override float preferredHeight
		{
			get
			{
				var prefHeight = textHeight;
				if( style.background != null )
					prefHeight = Math.Max( prefHeight + style.background.bottomHeight + style.background.topHeight, style.background.minHeight );

				return prefHeight;
			}
		}

		/// <summary>
		/// the maximum distance outside the TextField the mouse can move when pressing it to cause it to be unfocused
		/// </summary>
		public float textFieldBoundaryThreshold = 100f;

		/// <summary>
		/// if true and setText is called it will be ignored
		/// </summary>
		public bool shouldIgnoreTextUpdatesWhileFocused = true;

		protected string text;
		protected int cursor, selectionStart;
		protected bool hasSelection;
		protected bool writeEnters;
		List<float> glyphPositions = new List<float>( 15 );

		float _preferredWidth = 150;
		TextFieldStyle style;
		string messageText;
		protected string displayText = string.Empty;
		ITextFieldFilter filter;
		bool focusTraversal = true, onlyFontChars = true, disabled;
		int textHAlign = AlignInternal.left;
		float selectionX, selectionWidth;

		bool passwordMode;
		StringBuilder passwordBuffer;
		char passwordCharacter = '*';

		protected float fontOffset, textHeight, textOffset;
		float renderOffset;
		int visibleTextStart, visibleTextEnd;
		int maxLength = 0;

		float blinkTime = 0.5f;
		bool cursorOn = true;
		float lastBlink;

		bool programmaticChangeEvents;

		protected bool _isOver, _isPressed, _isFocused;
		ITimer _keyRepeatTimer;
		float _keyRepeatTime = 0.2f;


		public TextField( string text, TextFieldStyle style )
		{
			setStyle( style );
			setText( text );
			setSize( preferredWidth, preferredHeight );
		}


		public TextField( string text, Skin skin, string styleName = null ) : this( text, skin.get<TextFieldStyle>( styleName ) )
		{}


		#region IInputListener

		float _clickCountInterval = 0.2f;
		int _clickCount;
		float _lastClickTime;

		void IInputListener.onMouseEnter()
		{
			_isOver = true;
		}


		void IInputListener.onMouseExit()
		{
			_isOver = _isPressed = false;
		}


		bool IInputListener.onMousePressed( Vector2 mousePos )
		{
			if( disabled )
				return false;

			_isPressed = true;
			setCursorPosition( mousePos.X, mousePos.Y );
			selectionStart = cursor;
			hasSelection = true;
			var stage = getStage();
			if( stage != null )
				stage.setKeyboardFocus( this as IKeyboardListener );

			return true;
		}


		void IInputListener.onMouseMoved( Vector2 mousePos )
		{
			if( distanceOutsideBoundsToPoint( mousePos ) > textFieldBoundaryThreshold )
			{
				_isPressed = _isOver = false;
				getStage().removeInputFocusListener( this );
			}
			else
			{
				setCursorPosition( mousePos.X, mousePos.Y );
			}
		}


		void IInputListener.onMouseUp( Vector2 mousePos )
		{
			if( selectionStart == cursor )
				hasSelection = false;
			
			if( Time.time - _lastClickTime > _clickCountInterval )
				_clickCount = 0;
			_clickCount++;
			_lastClickTime = Time.time;
			_isPressed = _isOver = false;
		}

		#endregion


		#region IKeyboardListener

		void IKeyboardListener.keyDown( Keys key )
		{
			if( disabled )
				return;

			lastBlink = 0;
			cursorOn = false;

			var isCtrlDown = InputUtils.isControlDown();
			var jump = isCtrlDown && !passwordMode;
			var repeat = false;

			if( isCtrlDown )
			{
				if( key == Keys.V )
				{
					paste( Clipboard.getContents(), true );
				}
				else if( key == Keys.C || key == Keys.Insert )
				{
					copy();
					return;
				}
				else if( key == Keys.X )
				{
					cut( true );
					return;
				}
				else if( key == Keys.A )
				{
					selectAll();
					return;
				}
			}

			if( InputUtils.isShiftDown() )
			{
				if( key == Keys.Insert )
					paste( Clipboard.getContents(), true );
				else if( key == Keys.Delete )
					cut( true );

				// jumping around shortcuts
				var temp = cursor;
				var foundJumpKey = true;

				if( key == Keys.Left )
				{
					moveCursor( false, jump );
					repeat = true;
				}
				else if( key == Keys.Right )
				{
					moveCursor( true, jump );
					repeat = true;
				}
				else if( key == Keys.Home )
				{
					goHome();
				}
				else if( key == Keys.End )
				{
					goEnd();
				}
				else
				{
					foundJumpKey = false;
				}

				if( foundJumpKey && !hasSelection )
				{
					selectionStart = temp;
					hasSelection = true;
				}
			}
			else
			{
				// Cursor movement or other keys (kills selection)
				if( key == Keys.Left )
				{
					moveCursor( false, jump );
					clearSelection();
					repeat = true;
				}
				else if( key == Keys.Right )
				{
					moveCursor( true, jump );
					clearSelection();
					repeat = true;
				}
				else if( key == Keys.Home )
				{
					goHome();
				}
				else if( key == Keys.End )
				{
					goEnd();
				}
			}

			cursor = Mathf.clamp( cursor, 0, text.Length );

			if( repeat )
			{
				if( _keyRepeatTimer != null )
					_keyRepeatTimer.stop();
				_keyRepeatTimer = Core.schedule( _keyRepeatTime, true, this, t => ( t.context as IKeyboardListener ).keyDown( key ) );
			}
		}


		void IKeyboardListener.keyPressed( Keys key, char character )
		{
			if( InputUtils.isControlDown() )
				return;

			// disallow typing most ASCII control characters, which would show up as a space
			switch ( key )
			{
				case Keys.Back:
				case Keys.Delete:
				case Keys.Tab:
				case Keys.Enter:
				break;
				default:
					{
						if( (int)character < 32 )
							return;
						break;
					}
			}

			if( key == Keys.Tab && focusTraversal )
			{
				next( InputUtils.isShiftDown() );
			}
			else
			{
				var enterPressed = key == Keys.Enter;
				var backspacePressed = key == Keys.Back;
				var deletePressed = key == Keys.Delete;
				var add = enterPressed ? writeEnters : ( !onlyFontChars || style.font.hasCharacter( character ) );
				var remove = backspacePressed || deletePressed;

				if( add || remove )
				{
					var oldText = text;
					if( hasSelection )
					{
						cursor = delete( false );
					}
					else
					{
						if( backspacePressed && cursor > 0 )
						{
							text = text.Substring( 0, cursor - 1 ) + text.Substring( cursor-- );
							renderOffset = 0;
						}

						if( deletePressed && cursor < text.Length )
						{
							text = text.Substring( 0, cursor ) + text.Substring( cursor + 1 );
						}
					}

					if( add && !remove )
					{
						// character may be added to the text.
						if( !enterPressed && filter != null && !filter.acceptChar( this, character ) )
							return;

						if( !withinMaxLength( text.Length ) )
							return;
						
						var insertion = enterPressed ? "\n" : character.ToString();
						text = insert( cursor++, insertion, text );
					}

					changeText( oldText, text );
					updateDisplayText();
				}
			}
		}


		void IKeyboardListener.keyReleased( Keys key )
		{
			if( _keyRepeatTimer != null )
			{
				_keyRepeatTimer.stop();
				_keyRepeatTimer = null;
			}
		}


		void IKeyboardListener.gainedFocus()
		{
			hasSelection = _isFocused = true;
		}


		void IKeyboardListener.lostFocus()
		{
			hasSelection = _isFocused = false;
			if( _keyRepeatTimer != null )
			{
				_keyRepeatTimer.stop();
				_keyRepeatTimer = null;
			}
		}

		#endregion


		protected int letterUnderCursor( float x )
		{
			var halfSpaceSize = style.font.spaceWidth;
			x -= textOffset + fontOffset + halfSpaceSize /*- style.font.getData().cursorX*/ - glyphPositions[visibleTextStart];
			var n = glyphPositions.Count;
			for( var i = 0; i < n; i++ )
			{
				if( glyphPositions[i] > x && i >= 1 )
				{
					if( glyphPositions[i] - x <= x - glyphPositions[i - 1] )
						return i;
					return i - 1;
				}
			}
			return n - 1;
		}


		protected bool isWordCharacter( char c )
		{
			return ( c >= 'A' && c <= 'Z' ) || ( c >= 'a' && c <= 'z' ) || ( c >= '0' && c <= '9' );
		}


		protected int[] wordUnderCursor( int at )
		{
			int start = at, right = text.Length, left = 0, index = start;
			for(; index < right; index++ )
			{
				if( !isWordCharacter( text[index] ) )
				{
					right = index;
					break;
				}
			}
			for( index = start - 1; index > -1; index-- )
			{
				if( !isWordCharacter( text[index] ) )
				{
					left = index + 1;
					break;
				}
			}
			return new int[] { left, right };
		}


		int[] wordUnderCursor( float x )
		{
			return wordUnderCursor( letterUnderCursor( x ) );
		}


		bool withinMaxLength( int size )
		{
			return maxLength <= 0 || size < maxLength;
		}


		public void setMaxLength( int maxLength )
		{
			this.maxLength = maxLength;
		}


		public int getMaxLength()
		{
			return this.maxLength;
		}


		/// <summary>
		/// When false, text set by {@link #setText(String)} may contain characters not in the font, a space will be displayed instead.
		/// When true (the default), characters not in the font are stripped by setText. Characters not in the font are always stripped
		/// when typed or pasted.
		/// </summary>
		/// <param name="onlyFontChars">If set to <c>true</c> only font chars.</param>
		public void setOnlyFontChars( bool onlyFontChars )
		{
			this.onlyFontChars = onlyFontChars;
		}


		public void setStyle( TextFieldStyle style )
		{
			this.style = style;
			textHeight = style.font.lineHeight;
			invalidateHierarchy();
		}


		/// <summary>
		/// Returns the text field's style. Modifying the returned style may not have an effect until {@link #setStyle(TextFieldStyle)} is called
		/// </summary>
		/// <returns>The style.</returns>
		public TextFieldStyle getStyle()
		{
			return style;
		}


		protected void calculateOffsets()
		{
			float visibleWidth = getWidth();
			if( style.background != null )
				visibleWidth -= style.background.leftWidth + style.background.rightWidth;

			var glyphCount = glyphPositions.Count;

			// Check if the cursor has gone out the left or right side of the visible area and adjust renderoffset.
			var distance = glyphPositions[Math.Max( 0, cursor - 1 )] + renderOffset;
			if( distance <= 0 )
			{
				renderOffset -= distance;
			}
			else
			{
				var index = Math.Min( glyphCount - 1, cursor + 1 );
				var minX = glyphPositions[index] - visibleWidth;
				if( -renderOffset < minX )
				{
					renderOffset = -minX;
				}
			}

			// calculate first visible char based on render offset
			visibleTextStart = 0;
			var startX = 0f;
			for( var i = 0; i < glyphCount; i++ )
			{
				if( glyphPositions[i] >= -renderOffset )
				{
					visibleTextStart = Math.Max( 0, i );
					startX = glyphPositions[i];
					break;
				}
			}

			// calculate last visible char based on visible width and render offset
			var length = displayText.Length;
			visibleTextEnd = Math.Min( length, cursor + 1 );
			for(; visibleTextEnd <= length; visibleTextEnd++ )
				if( glyphPositions[visibleTextEnd] > startX + visibleWidth )
					break;
			visibleTextEnd = Math.Max( 0, visibleTextEnd - 1 );

			if( ( textHAlign & AlignInternal.left ) == 0 )
			{
				textOffset = visibleWidth - ( glyphPositions[visibleTextEnd] - startX );
				if( ( textHAlign & AlignInternal.center ) != 0 )
					textOffset = Mathf.round( textOffset * 0.5f );
			}
			else
			{
				textOffset = startX + renderOffset;
			}

			// calculate selection x position and width
			if( hasSelection )
			{
				var minIndex = Math.Min( cursor, selectionStart );
				var maxIndex = Math.Max( cursor, selectionStart );
				var minX = Math.Max( glyphPositions[minIndex], -renderOffset );
				var maxX = Math.Min( glyphPositions[maxIndex], visibleWidth - renderOffset );
				selectionX = minX;

				if( renderOffset == 0 )
					selectionX += textOffset;

				selectionWidth = maxX - minX;
			}
		}


		#region Drawing

		public override void draw( Graphics graphics, float parentAlpha )
		{
			var font = style.font;
			var fontColor = ( disabled && style.disabledFontColor.HasValue ) ? style.disabledFontColor.Value
                : ( ( _isFocused && style.focusedFontColor.HasValue ) ? style.focusedFontColor.Value : style.fontColor );
			IDrawable selection = style.selection;
			IDrawable background = ( disabled && style.disabledBackground != null ) ? style.disabledBackground
				: ( ( _isFocused && style.focusedBackground != null ) ? style.focusedBackground : style.background );

			var color = getColor();
			var x = getX();
			var y = getY();
			var width = getWidth();
			var height = getHeight();

			float bgLeftWidth = 0, bgRightWidth = 0;
			if( background != null )
			{
				background.draw( graphics, x, y, width, height, new Color( color, color.A * parentAlpha ) );
				bgLeftWidth = background.leftWidth;
				bgRightWidth = background.rightWidth;
			}

			var textY = getTextY( font, background );
			calculateOffsets();

			if( _isFocused && hasSelection && selection != null )
				drawSelection( selection, graphics, font, x + bgLeftWidth, y + textY );

			//float yOffset = font.isFlipped() ? -textHeight : 0;
			float yOffset = 0;
			if( displayText.Length == 0 )
			{
				if( !_isFocused && messageText != null )
				{
					var messageFontColor = style.messageFontColor.HasValue ? style.messageFontColor.Value : new Color( 180, 180, 180, color.A * parentAlpha );
					var messageFont = style.messageFont != null ? style.messageFont : font;
					graphics.batcher.drawString( messageFont, messageText, new Vector2( x + bgLeftWidth, y + textY + yOffset ), messageFontColor );
					//messageFont.draw( graphics.batcher, messageText, x + bgLeftWidth, y + textY + yOffset, 0, messageText.length(),
					//	width - bgLeftWidth - bgRightWidth, textHAlign, false, "..." );
				}
			}
			else
			{
				var col = new Color( fontColor, fontColor.A * parentAlpha );
				var t = displayText.Substring( visibleTextStart, visibleTextEnd - visibleTextStart );
				graphics.batcher.drawString( font, t, new Vector2( x + bgLeftWidth + textOffset, y + textY + yOffset ), col );
			}

			if( _isFocused && !disabled )
			{
				blink();
				if( cursorOn && style.cursor != null )
					drawCursor( style.cursor, graphics, font, x + bgLeftWidth, y + textY );
			}
		}


		protected float getTextY( BitmapFont font, IDrawable background )
		{
			float height = getHeight();
			float textY = textHeight / 2 + font.descent;
			if( background != null )
			{
				var bottom = background.bottomHeight;
				textY = textY - ( height - background.topHeight - bottom ) / 2 + bottom;
			}
			else
			{
				textY = textY - height / 2;
			}
				
			return textY;
		}


		/// <summary>
		/// Draws selection rectangle
		/// </summary>
		/// <param name="selection">Selection.</param>
		/// <param name="batch">Batch.</param>
		/// <param name="font">Font.</param>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		protected void drawSelection( IDrawable selection, Graphics graphics, BitmapFont font, float x, float y )
		{
			selection.draw( graphics, x + selectionX + renderOffset + fontOffset, y - font.descent / 2, selectionWidth, textHeight, Color.White );
		}


		protected void drawCursor( IDrawable cursorPatch, Graphics graphics, BitmapFont font, float x, float y )
		{
			cursorPatch.draw( graphics,
				x + textOffset + glyphPositions[cursor] - glyphPositions[visibleTextStart] + fontOffset - 1 /*font.getData().cursorX*/,
				y - font.descent / 2, cursorPatch.minWidth, textHeight, color );
		}

		#endregion


		void updateDisplayText()
		{
			var textLength = text.Length;

			var buffer = new StringBuilder();
			for( var i = 0; i < textLength; i++ )
			{
				var c = text[i];
				buffer.Append( style.font.hasCharacter( c ) ? c : ' ' );
			}
			var newDisplayText = buffer.ToString();

			if( passwordMode && style.font.hasCharacter( passwordCharacter ) )
			{
				if( passwordBuffer == null )
					passwordBuffer = new StringBuilder( newDisplayText.Length );
				else if( passwordBuffer.Length > textLength )
					passwordBuffer.Clear();

				for( var i = passwordBuffer.Length; i < textLength; i++ )
					passwordBuffer.Append( passwordCharacter );
				displayText = passwordBuffer.ToString();
			}
			else
			{
				displayText = newDisplayText;
			}

			//layout.setText( font, displayText );
			glyphPositions.Clear();
			float x = 0;
			if( displayText.Length > 0 )
			{
				for( var i = 0; i < displayText.Length; i++ )
				{
					var region = style.font.fontRegionForChar( displayText[i] );
					// we dont have fontOffset in BitmapFont, it is the first Glyph in a GlyphRun
					//if( i == 0 )
					//	fontOffset = region.xAdvance;
					glyphPositions.Add( x );
					x += region.xAdvance;
				}
				//GlyphRun run = layout.runs.first();
				//FloatArray xAdvances = run.xAdvances;
				//fontOffset = xAdvances.first();
				//for( int i = 1, n = xAdvances.size; i < n; i++ )
				//{
				//	glyphPositions.add( x );
				//	x += xAdvances.get( i );
				//}
			}
			else
			{
				fontOffset = 0;
			}
			glyphPositions.Add( x );

			if( selectionStart > newDisplayText.Length )
				selectionStart = textLength;
		}


		void blink()
		{
			if( ( Time.time - lastBlink ) > blinkTime )
			{
				cursorOn = !cursorOn;
				lastBlink = Time.time;
			}
		}


		#region Text manipulation

		/// <summary>
		/// Copies the contents of this TextField to the {@link Clipboard} implementation set on this TextField
		/// </summary>
		public void copy()
		{
			if( hasSelection && !passwordMode )
			{
				var start = Math.Min( cursor, selectionStart );
				var length = Math.Max( cursor, selectionStart ) - start;
				Clipboard.setContents( text.Substring( start, length ) );
			}
		}


		/// <summary>
		/// Copies the selected contents of this TextField to the {@link Clipboard} implementation set on this TextField, then removes it
		/// </summary>
		public void cut()
		{
			cut( programmaticChangeEvents );
		}


		void cut( bool fireChangeEvent )
		{
			if( hasSelection && !passwordMode )
			{
				copy();
				cursor = delete( fireChangeEvent );
				updateDisplayText();
			}
		}


		void paste( string content, bool fireChangeEvent )
		{
			if( content == null )
				return;

			var buffer = new StringBuilder();
			int textLength = text.Length;
			if( hasSelection )
				textLength -= Math.Abs( cursor - selectionStart );
			
			//var data = style.font.getData();
			for( int i = 0, n = content.Length; i < n; i++ )
			{
				if( !withinMaxLength( textLength + buffer.Length ) )
					break;

				var c = content[i];
				if( !( writeEnters && c == '\r' ) )
				{
					if( onlyFontChars && !style.font.hasCharacter( c ) )
						continue;
					
					if( filter != null && !filter.acceptChar( this, c ) )
						continue;
				}

				buffer.Append( c );
			}
			content = buffer.ToString();

			if( hasSelection )
				cursor = delete( fireChangeEvent );
			if( fireChangeEvent )
				changeText( text, insert( cursor, content, text ) );
			else
				text = insert( cursor, content, text );
			updateDisplayText();
			cursor += content.Length;
		}


		string insert( int position, string text, string to )
		{
			if( to.Length == 0 )
				return text;
			return to.Substring( 0, position ) + text + to.Substring( position, to.Length - position );
		}


		int delete( bool fireChangeEvent )
		{
			var from = selectionStart;
			var to = cursor;
			var minIndex = Math.Min( from, to );
			var maxIndex = Math.Max( from, to );
			var newText = ( minIndex > 0 ? text.Substring( 0, minIndex ) : "" )
			              + ( maxIndex < text.Length ? text.Substring( maxIndex, text.Length - maxIndex ) : "" );
			
			if( fireChangeEvent )
				changeText( text, newText );
			else
				text = newText;

			clearSelection();
			return minIndex;
		}


		/// <summary>
		/// Focuses the next TextField. If none is found, the keyboard is hidden. Does nothing if the text field is not in a stage
		/// up: If true, the TextField with the same or next smallest y coordinate is found, else the next highest.
		/// </summary>
		/// <param name="up">Up.</param>
		public void next( bool up )
		{
			var stage = getStage();
			if( stage == null )
				return;

			var tmp2 = Vector2.Zero;
			var tmp1 = getParent().localToStageCoordinates( new Vector2( getX(), getY() ) );
			var textField = findNextTextField( stage.getElements(), null, tmp2, tmp1, up );
			if( textField == null )
			{
				// Try to wrap around.
				if( up )
					tmp1 = new Vector2( float.MinValue, float.MinValue );
				else
					tmp1 = new Vector2( float.MaxValue, float.MaxValue );
				textField = findNextTextField( getStage().getElements(), null, tmp2, tmp1, up );
			}

			if( textField != null )
				stage.setKeyboardFocus( textField );
		}


		TextField findNextTextField( List<Element> elements, TextField best, Vector2 bestCoords, Vector2 currentCoords, bool up )
		{
			bestCoords = Vector2.Zero;
			for( int i = 0, n = elements.Count; i < n; i++ )
			{
				var element = elements[i];
				if( element == this )
					continue;
				
				if( element is TextField )
				{
					var textField = (TextField)element;
					if( textField.isDisabled() || !textField.focusTraversal )
						continue;
					
					var elementCoords = element.getParent().localToStageCoordinates( new Vector2( element.getX(), element.getY() ) );
					if( ( elementCoords.Y < currentCoords.Y || ( elementCoords.Y == currentCoords.Y && elementCoords.X > currentCoords.X ) ) ^ up )
					{
						if( best == null
						    || ( elementCoords.Y > bestCoords.Y || ( elementCoords.Y == bestCoords.Y && elementCoords.X < bestCoords.X ) ) ^ up )
						{
							best = (TextField)element;
							bestCoords = elementCoords;
						}
					}
				}
				else if( element is Group )
				{
					best = findNextTextField( ( (Group)element ).getChildren(), best, bestCoords, currentCoords, up );
				}
			}

			return best;
		}


		#endregion


		/// <summary>
		/// if str is null, "" is used
		/// </summary>
		/// <param name="str">String.</param>
		public void appendText( string str )
		{
			if( shouldIgnoreTextUpdatesWhileFocused && _isFocused )
				return;
			
			if( str == null )
				str = "";

			clearSelection();
			cursor = text.Length;
			paste( str, programmaticChangeEvents );
		}


		/// <summary>
		/// str If null, "" is used
		/// </summary>
		/// <param name="str">String.</param>
		public TextField setText( string str )
		{
			if( shouldIgnoreTextUpdatesWhileFocused && _isFocused )
				return this;
			
			if( str == null )
				str = "";
			if( str == text )
				return this;

			clearSelection();
			var oldText = text;
			text = "";
			paste( str, false );
			if( programmaticChangeEvents )
				changeText( oldText, text );
			cursor = 0;

			return this;
		}


		/// <summary>
		/// force sets the text without validating or firing change events. Use at your own risk.
		/// </summary>
		/// <returns>The text forced.</returns>
		/// <param name="str">String.</param>
		public TextField setTextForced( string str )
		{
			text = str;
			updateDisplayText();
			return this;
		}


		/// <summary>
		/// Never null, might be an empty string
		/// </summary>
		/// <returns>The text.</returns>
		public string getText()
		{
			return text;
		}


		/// <summary>
		/// oldText May be null
		/// </summary>
		/// <param name="oldText">Old text.</param>
		/// <param name="newText">New text.</param>
		void changeText( string oldText, string newText )
		{
			if( newText == oldText )
				return;
			text = newText;

			if( onTextChanged != null )
				onTextChanged( this, text );
		}


		/// <summary>
		/// If false, methods that change the text will not fire {@link onTextChanged}, the event will be fired only when user changes the text
		/// </summary>
		/// <param name="programmaticChangeEvents">If set to <c>true</c> programmatic change events.</param>
		public TextField setProgrammaticChangeEvents( bool programmaticChangeEvents )
		{
			this.programmaticChangeEvents = programmaticChangeEvents;
			return this;
		}


		public int getSelectionStart()
		{
			return selectionStart;
		}


		public string getSelection()
		{
			return hasSelection ? text.Substring( Math.Min( selectionStart, cursor ), Math.Max( selectionStart, cursor ) ) : "";
		}


		/// <summary>
		/// Sets the selected text
		/// </summary>
		/// <param name="selectionStart">Selection start.</param>
		/// <param name="selectionEnd">Selection end.</param>
		public TextField setSelection( int selectionStart, int selectionEnd )
		{
			Assert.isFalse( selectionStart < 0, "selectionStart must be >= 0" );
			Assert.isFalse( selectionEnd < 0, "selectionEnd must be >= 0" );

			selectionStart = Math.Min( text.Length, selectionStart );
			selectionEnd = Math.Min( text.Length, selectionEnd );
			if( selectionEnd == selectionStart )
			{
				clearSelection();
				return this;
			}

			if( selectionEnd < selectionStart )
			{
				int temp = selectionEnd;
				selectionEnd = selectionStart;
				selectionStart = temp;
			}

			hasSelection = true;
			this.selectionStart = selectionStart;
			cursor = selectionEnd;

			return this;
		}


		public void selectAll()
		{
			setSelection( 0, text.Length );
		}


		public void clearSelection()
		{
			hasSelection = false;
		}


		protected void setCursorPosition( float x, float y )
		{
			lastBlink = 0;
			cursorOn = false;
			cursor = letterUnderCursor( x );
		}


		/// <summary>
		/// Sets the cursor position and clears any selection
		/// </summary>
		/// <param name="cursorPosition">Cursor position.</param>
		public void setCursorPosition( int cursorPosition )
		{
			Assert.isFalse( cursorPosition < 0, "cursorPosition must be >= 0" );
			clearSelection();
			cursor = Math.Min( cursorPosition, text.Length );
		}


		public int getCursorPosition()
		{
			return cursor;
		}


		protected void goHome()
		{
			cursor = 0;
		}


		protected void goEnd()
		{
			cursor = text.Length;
		}


		protected void moveCursor( bool forward, bool jump )
		{
			var limit = forward ? text.Length : 0;
			var charOffset = forward ? 0 : -1;

			if( ( forward && cursor == limit ) || ( !forward && cursor == 0 ) )
				return;

			while( ( forward ? ++cursor < limit : --cursor > limit ) && jump )
			{
				if( !continueCursor( cursor, charOffset ) )
					break;
			}
		}


		protected bool continueCursor( int index, int offset )
		{
			var c = text[index + offset];
			return isWordCharacter( c );
		}


		#region Configuration

		public TextField setPreferredWidth( float preferredWidth )
		{
			_preferredWidth = preferredWidth;
			return this;
		}


		/// <summary>
		/// filter May be null
		/// </summary>
		/// <param name="filter">Filter.</param>
		public TextField setTextFieldFilter( ITextFieldFilter filter )
		{
			this.filter = filter;
			return this;
		}


		public ITextFieldFilter getTextFieldFilter()
		{
			return filter;
		}


		/// <summary>
		/// If true (the default), tab/shift+tab will move to the next text field
		/// </summary>
		/// <param name="focusTraversal">If set to <c>true</c> focus traversal.</param>
		public TextField setFocusTraversal( bool focusTraversal )
		{
			this.focusTraversal = focusTraversal;
			return this;
		}


		/// <summary>
		/// May be null
		/// </summary>
		/// <returns>The message text.</returns>
		public string getMessageText()
		{
			return messageText;
		}


		/// <summary>
		/// Sets the text that will be drawn in the text field if no text has been entered.
		/// </summary>
		/// <param name="messageText">Message text.</param>
		public TextField setMessageText( string messageText )
		{
			this.messageText = messageText;
			return this;
		}


		/// <summary>
		/// Sets text horizontal alignment (left, center or right).
		/// </summary>
		/// <param name="alignment">Alignment.</param>
		public TextField setAlignment( Align alignment )
		{
			this.textHAlign = (int)alignment;
			return this;
		}


		/// <summary>
		/// If true, the text in this text field will be shown as bullet characters.
		/// </summary>
		/// <param name="passwordMode">Password mode.</param>
		public TextField setPasswordMode( bool passwordMode )
		{
			this.passwordMode = passwordMode;
			updateDisplayText();
			return this;
		}


		public bool isPasswordMode()
		{
			return passwordMode;
		}


		/// <summary>
		/// Sets the password character for the text field. The character must be present in the {@link BitmapFont}. Default is 149 (bullet)
		/// </summary>
		/// <param name="passwordCharacter">Password character.</param>
		public void setPasswordCharacter( char passwordCharacter )
		{
			this.passwordCharacter = passwordCharacter;
			if( passwordMode )
				updateDisplayText();
		}


		public TextField setBlinkTime( float blinkTime )
		{
			this.blinkTime = blinkTime;
			return this;
		}


		public TextField setDisabled( bool disabled )
		{
			this.disabled = disabled;
			return this;
		}


		public bool isDisabled()
		{
			return disabled;
		}

		#endregion


		/// <summary>
		/// Interface for filtering characters entered into the text field.
		/// </summary>
		public interface ITextFieldFilter
		{
			bool acceptChar( TextField textField, char c );
		}
	}


	public class TextFieldStyle
	{
		public BitmapFont font;
		public Color fontColor = Color.White;
		/** Optional. */
		public Color? focusedFontColor, disabledFontColor;
		/** Optional. */
		public IDrawable background, focusedBackground, disabledBackground, cursor, selection;
		/** Optional. */
		public BitmapFont messageFont;
		/** Optional. */
		public Color? messageFontColor;


		public TextFieldStyle()
		{
			font = Graphics.instance.bitmapFont;
		}


		public TextFieldStyle( BitmapFont font, Color fontColor, IDrawable cursor, IDrawable selection, IDrawable background )
		{
			this.background = background;
			this.cursor = cursor;
			this.font = font ?? Graphics.instance.bitmapFont;
			this.fontColor = fontColor;
			this.selection = selection;
		}


		public static TextFieldStyle create( Color fontColor, Color cursorColor, Color selectionColor, Color backgroundColor )
		{
			var cursor = new PrimitiveDrawable( cursorColor );
			cursor.minWidth = 1;
			cursor.leftWidth = 4;

			var background = new PrimitiveDrawable( backgroundColor );
			background.leftWidth = background.rightWidth = 10f;
			background.bottomHeight = background.topHeight = 5f;

			return new TextFieldStyle {
				fontColor = fontColor,
				cursor = cursor,
				selection = new PrimitiveDrawable( selectionColor ),
				background = background
			};
		}
	

		public TextFieldStyle clone()
		{
			return new TextFieldStyle {
				font = font,
				fontColor = fontColor,
				focusedFontColor = focusedFontColor,
				disabledFontColor = disabledFontColor,
				background = background,
				focusedBackground = focusedBackground,
				disabledBackground = disabledBackground,
				cursor = cursor,
				selection = selection,
				messageFont = messageFont,
				messageFontColor = messageFontColor
			};
		}
	}


	public class DigitsOnlyFilter : TextField.ITextFieldFilter
	{
		public bool acceptChar( TextField textField, char c )
		{
			return Char.IsDigit( c ) || c == '-';
		}
	}


	public class FloatFilter : TextField.ITextFieldFilter
	{
		public bool acceptChar( TextField textField, char c )
		{
			// only allow one .
			if( c == '.' )
				return !textField.getText().Contains( "." );
			return Char.IsDigit( c ) || c == '-';
		}
	}


	public class BoolFilter : TextField.ITextFieldFilter
	{
		public bool acceptChar( TextField textField, char c )
		{
			if( c == 't' )
				textField.setTextForced( "true" );

			if( c == 'f' )
				textField.setTextForced( "false" );

			return false;
		}
	}

}

