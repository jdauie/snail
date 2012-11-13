using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MarkupParser.Nodes;

namespace MarkupParser
{
	internal class TokenGroup
	{
		public readonly NodeType Type;
		public readonly bool IsClosing;
		public bool IsEmpty;
		public readonly int TokenStartIndex;
		public readonly int TokenEndIndex;
		public short TreeDepth;

		public TokenGroup(NodeType type, bool isClosing, bool isEmpty, int start, int end, short depth = 0)
		{
			Type = type;
			IsClosing = isClosing;
			IsEmpty = isEmpty;
			TokenStartIndex = start;
			TokenEndIndex = end;
			TreeDepth = depth;
		}

		public override string ToString()
		{
			return string.Format("{0}{1}", "".PadRight(TreeDepth * 2), (IsEmpty ? "[]" : (IsClosing ? "]" : "[")));
		}
	}

	class Parser
	{
		public static List<TokenGroup> ParseTokens(TokenList tokens)
		{
			var taglist = new List<TokenGroup>();
		
			// temporary variables
			var length  = tokens.Tokens.Count;
		
			// loop through tokens and itentify tags
			for(int i = 0; i < length; i++) {
			    // constant token identifier
			    if(tokens[i].Type != TokenType.CDATA) {
			        // begin with START_TAG
			        if(tokens[i].Type == TokenType.START_TAG) {
			            // find the next END_TAG
			            var j = i + 1;
			            while(j < length && tokens[j].Type != TokenType.END_TAG) {
			                ++j;
			            }
			            
						// default tag values
			            var type    = NodeType.ELEMENT_NODE;
			            var closing = false; // closing tag
			            var start   = i + 1;     // index of start position in token array
			            var end     = j - 1;     // index of end position in token array
			            
						// check for different type identifiers and change the 
			            // type, start, and end values if necessary
			            if(tokens[i + 1].Type != TokenType.CDATA) {
			                // tag format "<!...>"
			                if(tokens[i + 1].Type == TokenType.EXCLAMATION_POINT) {
			                    start += 2;
			                    // tag format "<!--...>" or "<![CDATA[...>"
			                    if(tokens[i + 2].Type != TokenType.CDATA) {
			                        // tag format "<![CDATA[...>"
			                        if(tokens[i + 2].Type == TokenType.START_CDATA) {
			                            end -= 1;
			                            type = NodeType.CDATA_SECTION_NODE;
			                        // tag format "<!--...>"
			                        } else if(tokens[i + 2].Type == TokenType.START_COMMENT) {
			                            end -= 1;
			                            type = NodeType.COMMENT_NODE;
			                        }
			                    // tag format "<!DOCTYPE...>"
			                    } else if(String.Compare(tokens[i + 2].Value, "DOCTYPE", true) == 0) {
			                        type = NodeType.DOCUMENT_TYPE_NODE;
			                    } else if(String.Compare(tokens[i + 2].Value, "ENTITY", true) == 0) {
			                        type = NodeType.DTD_ENTITY_NODE;
			                    } else if(String.Compare(tokens[i + 2].Value, "ELEMENT", true) == 0) {
			                        type = NodeType.DTD_ELEMENT_NODE;
			                    } else if(String.Compare(tokens[i + 2].Value, "ATTLIST", true) == 0) {
			                        type = NodeType.DTD_ATTLIST_NODE;
			                    }
			                // tag format "<?...>"
			                } else if(tokens[i + 1].Type == TokenType.START_PROCESSING_INSTRUCTION) {
			                    start += 1;
			                    end   -= 1;
			                    type   = NodeType.PROCESSING_INSTRUCTION_NODE;
			                // tag format "</...>"
			                } else if(tokens[i + 1].Type == TokenType.FORWARD_SLASH) {
			                    start  += 1;
			                    closing = true;
			                }
			            }
			            // create node
						taglist.Add(new TokenGroup(type, closing, false, start, end));
			            i = j;
			        }
			    // CDATA (string) token
			    } else {
			        var j = i + 1;
			        // read tokens until a start tag is found.
			        while(j < length && tokens[j].Type != TokenType.START_TAG) {
			            ++j;
			        }
			        // create textnode
					taglist.Add(new TokenGroup(NodeType.TEXT_NODE, false, true, i, j - 1));
			        // update counter so that the next iteration is on the START_TAG
			        i = j - 1;
			    }
			}
		
			return taglist;
		}
	
		public static void CalculateTagDepths(TokenList tokens, List<TokenGroup> taglist) {
		
			// temporary variables
			var length = taglist.Count;
			var depth  = (short)0;
			var stack  = new Stack<string>();

			foreach (var tag in taglist)
			{
			    if(tag.Type == NodeType.ELEMENT_NODE) {
			        if(tag.IsClosing) {
			            // check for matching start tag on the stack
			            if(String.Compare(stack.Peek(), tokens[tag.TokenStartIndex].Value, true) == 0) {
			                --depth;
			                tag.TreeDepth = depth;
			                stack.Pop();
			            } else {
			                // the end tag does not match the start tag on the stack, 
			                // so the following are possibilities (within the scope):
			                // 1) the end tag has no opening tag
			                // 2) the previous opening tag has no closing tag
			                /**
			                 * @todo this does not yet take into account situations 
			                 * like old html lists where <li>'s were not closed
			                 */
			                tag.TreeDepth = -1;
			            }
			        } else if(Config.EmptyTags.Contains(tokens[tag.TokenStartIndex].Value)) {
			            tag.TreeDepth = depth;
			            tag.IsEmpty = true;
			        } else {
			            stack.Push(tokens[tag.TokenStartIndex].Value);
			            tag.TreeDepth = depth;
			            ++depth;
			        }
			    } else {
			        tag.TreeDepth = depth;
			    }
			}
		}

		public static DocumentNode ParseTagsToTree(TokenList tokens, List<TokenGroup> taglist)
		{
			// temporary variables
			var root = new DocumentNode();
			ElementNode current = root;
			Node node = null;

			// loop through final tag array and create the tree
			// this is separate from setting the depth so that errors can 
			// be isolated in that section
			foreach(var tag in taglist){
			    node = null;
			    if(tag.TreeDepth != -1) {
			        if(tag.Type == NodeType.ELEMENT_NODE) {
			            if(tag.IsClosing) {
			                current = current.Parent;
			            } else if(tag.IsEmpty) {
			                node = new ElementNode(tokens[tag.TokenStartIndex].Value, true);
			                current.AppendChild(node);
			            } else {
							var elementNode = new ElementNode(tokens[tag.TokenStartIndex].Value, false);
							node = elementNode;
			                current.AppendChild(node);
							current = elementNode;
			            }
			            if(node != null) {
			                // add attributes
			                for(int j = tag.TokenStartIndex + 1; j <= tag.TokenEndIndex; j++) {
			                    if(tokens[j].Type == TokenType.CDATA) {
			                        var attribute = new NodeAttribute(tokens[j].Value, null);
			                        ++j;
			                        if( j < tag.TokenEndIndex && 
			                            tokens[j].Type == TokenType.EQUALS_SIGN ) {
			                            ++j;
			                            if(tokens[j].Type == TokenType.START_QUOTE) {
			                                ++j;
			                                if(j < tag.TokenEndIndex) {
			                                    if(tokens[j].Type == TokenType.END_QUOTE) {
			                                        // blank value
			                                        attribute.Value = "";
			                                    } else {
			                                        // standard quoted value
			                                        attribute.Value = tokens[j].Value;
			                                        ++j;
			                                    }
			                                } else {
			                                    // half-quoted value (trailing off end of document?)
			                                    //this->insertLogEntry(ERROR, ATTRIBUTE_FORMAT, MISSING_END_QUOTE, null);
			                                    attribute.Value = tokens[j].Value;
			                                    ++j;
			                                }
			                            } else {
			                                // unquoted value
			                                attribute.Value = tokens[j].Value;
			                            }
			                        } else {
			                            //debugval = Parser::reassembleTokenString(array_slice(tokens, j - 1, 1));
			                            //this->insertLogEntry(WARNING, ATTRIBUTE_FORMAT, MISSING_VALUE, "after '".debugval."'");
			                            --j;
			                        }
									
									node.Attributes.Add(attribute.Name, attribute.Value);

			                    } else if(tokens[j].Type == TokenType.FORWARD_SLASH) {
			                        // ignore for now
			                    } else {
			                        //int t = j;
			                        if(tokens[j].Type == TokenType.START_QUOTE) {
			                            while(tokens[j].Type != TokenType.END_QUOTE) {
			                                ++j;
			                            }
			                        } else {
			                            while(tokens[j].Type != TokenType.CDATA) {
			                                ++j;
			                            }
			                        }
			                        --j;
			                        //debugval = Parser::reassembleTokenString(array_slice(tokens, t, j - t + 1));
			                        //this->insertLogEntry(ERROR, ATTRIBUTE_FORMAT, INVALID_TOKEN_POSITION, "token sequence '".debugval."' removed");
			                    }
			                }
			            }
			        } else {
			            if(tag.Type == NodeType.TEXT_NODE) {
			                node = new TextNode(tokens[tag.TokenStartIndex].Value);
			            } else if(tag.Type == NodeType.COMMENT_NODE) {
			                node = new CommentNode(tokens[tag.TokenStartIndex].Value);
			            } else if(tag.Type == NodeType.CDATA_SECTION_NODE) {
			                node = new CDATASectionNode(tokens[tag.TokenStartIndex].Value);
			            } else if(tag.Type == NodeType.PROCESSING_INSTRUCTION_NODE) {
			                node = new ProcessingInstructionNode(tokens[tag.TokenStartIndex].Value);
			            } else if(tag.Type == NodeType.DOCUMENT_TYPE_NODE) {
			                // format: PUBLIC "public" "system"
			                // or      SYSTEM "system"
			                var name = tokens[tag.TokenStartIndex].Value;
			                var type = tokens[tag.TokenStartIndex + 1].Value;
			                string publicId = null;
			                string systemId = null;
			                int j = tag.TokenStartIndex + 2;
			                if(String.Compare(type, "PUBLIC", true) == 0) {
			                    if(tokens[j].Type == TokenType.START_QUOTE) {
			                        publicId = tokens[j + 1].Value;
			                        j += 3;
			                    }
			                }
			                if(tokens[j].Type == TokenType.START_QUOTE) {
			                    systemId = tokens[j + 1].Value;
			                }
			                node = new DocumentTypeNode(name, publicId, systemId);
			            } else if(tag.Type == NodeType.DTD_ENTITY_NODE) {
			                //node = new DTDEntityNode();
			            } else if(tag.Type == NodeType.DTD_ELEMENT_NODE) {
			                //node = new DTDElementNode();
			            } else if(tag.Type == NodeType.DTD_ATTLIST_NODE) {
			                //node = new DTDAttListNode();
			            }
			            if(node != null) {
			                current.AppendChild(node);
			            }
			        }
			    }
			}
		
			return root;
		}
	}
}
