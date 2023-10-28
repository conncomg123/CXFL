#include "../include/Element.h"
#include <stdexcept>
Element::Element() noexcept {
	this->selected = false;
	this->width = UNDEF;
	this->height = UNDEF;
}
Element::Element(pugi::xml_node& elementNode, std::string elementType) noexcept(false) : 
		matrix(elementNode.child("matrix").child("Matrix"), elementNode), 
		transformationPoint(elementNode.child("transformationPoint").child("Point")) {
	if(std::find(ACCEPTABLE_ELEMENT_TYPES.begin(), ACCEPTABLE_ELEMENT_TYPES.end(), elementType) == ACCEPTABLE_ELEMENT_TYPES.end()) throw std::invalid_argument("Invalid element type: " + elementType);
	this->elementType = elementType;
	this->root = elementNode;
	this->width = UNDEF;
	this->height = UNDEF;
}
Element::Element(const Element& element) noexcept : 
		matrix(element.getMatrix()), 
		transformationPoint(element.getTransformationPoint()) {
	auto parent = element.root.parent();
	this->elementType = element.elementType;
	this->root = parent.insert_copy_after(element.root, element.root);
	this->setWidth(element.getWidth());
	this->setHeight(element.getHeight());
	this->setSelected(element.isSelected());
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
bool Element::isSelected() const noexcept {
	return this->selected;
}
void Element::setSelected(bool selected) noexcept {
	if (!selected) this->root.remove_attribute("isSelected");
	else {
		if (this->root.attribute("isSelected").empty()) this->root.append_attribute("isSelected");
		this->root.attribute("isSelected").set_value(selected);
	}
	this->selected = selected;
}
Matrix& Element::getMatrix() noexcept {
	return this->matrix;
}
const Matrix& Element::getMatrix() const noexcept {
	return this->matrix;
}
void Element::setMatrix(const Matrix& matrix) noexcept {
	this->matrix = matrix;
}
Point& Element::getTransformationPoint() noexcept {
	return this->transformationPoint;
}
const Point& Element::getTransformationPoint() const noexcept {
	return this->transformationPoint;
}
void Element::setTransformationPoint(const Point& transformationPoint) noexcept {
	this->transformationPoint = transformationPoint;
}
pugi::xml_node& Element::getRoot() noexcept {
	return this->root;
}
const pugi::xml_node& Element::getRoot() const noexcept {
	return this->root;
}