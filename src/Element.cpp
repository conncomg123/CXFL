#include "../include/Element.h"
Element::Element() {
	this->width = UNDEF;
	this->height = UNDEF;
}
Element::Element(pugi::xml_node& elementNode) {
	this->root = elementNode;
	this->width = UNDEF;
	this->height = UNDEF;
}
Element::Element(const Element& element) {
	auto parent = element.root.parent();
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
std::string Element::getType() {
	return this->root.name();
}
pugi::xml_node& Element::getRoot() {
	return this->root;
}