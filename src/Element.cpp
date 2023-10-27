#include "../include/Element.h"
#include <stdexcept>
Element::Element() noexcept {
	this->width = UNDEF;
	this->height = UNDEF;
}
Element::Element(pugi::xml_node& elementNode, std::string elementType) noexcept(false) {
	if(std::find(ACCEPTABLE_ELEMENT_TYPES.begin(), ACCEPTABLE_ELEMENT_TYPES.end(), elementType) == ACCEPTABLE_ELEMENT_TYPES.end()) throw std::invalid_argument("Invalid element type: " + elementType);
	this->elementType = elementType;
	this->root = elementNode;
	this->width = UNDEF;
	this->height = UNDEF;
}
Element::Element(const Element& element) noexcept {
	auto parent = element.root.parent();
	this->elementType = element.elementType;
	this->root = parent.insert_copy_after(element.root, element.root);
	this->setWidth(element.getWidth());
	this->setHeight(element.getHeight());
}
Element::~Element() noexcept {
}
void Element::setWidth(double width) noexcept {
	this->width = width;
}
void Element::setHeight(double height) noexcept {
	this->height = height;
}
const std::string& Element::getElementType() const noexcept {
	return this->elementType;
}
pugi::xml_node& Element::getRoot() noexcept {
	return this->root;
}
const pugi::xml_node& Element::getRoot() const noexcept {
	return this->root;
}