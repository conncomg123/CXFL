#include "../include/Element.h"
#include <stdexcept>
Element::Element() {
	this->width = UNDEF;
	this->height = UNDEF;
}
Element::Element(pugi::xml_node& elementNode, std::string elementType) {
	if(std::find(ACCEPTABLE_ELEMENT_TYPES.begin(), ACCEPTABLE_ELEMENT_TYPES.end(), elementType) == ACCEPTABLE_ELEMENT_TYPES.end()) throw std::invalid_argument("Invalid element type: " + elementType);
	this->elementType = elementType;
	this->root = elementNode;
	this->width = UNDEF;
	this->height = UNDEF;
}
Element::Element(const Element& element) {
	auto parent = element.root.parent();
	this->elementType = element.elementType;
	this->root = parent.insert_copy_after(element.root, element.root);
	this->setWidth(element.getWidth());
	this->setHeight(element.getHeight());
}
Element::~Element() {
}
void Element::setWidth(double width) {
	this->width = width;
}
void Element::setHeight(double height) {
	this->height = height;
}
std::string Element::getElementType() {
	return this->elementType;
}
pugi::xml_node& Element::getRoot() {
	return this->root;
}