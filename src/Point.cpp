#include "../include/Point.h"
constexpr auto EPSILON = 0.0001;
Point::Point() noexcept {
	this->x = 0.0;
	this->y = 0.0;
}
Point::Point(pugi::xml_node& pointNode) noexcept {
	this->root = pointNode;
	this->x = pointNode.attribute("x").as_double();
	this->y = pointNode.attribute("y").as_double();
}
Point::Point(const pugi::xml_node& pointNode) noexcept {
	this->root = pointNode;
	this->x = pointNode.attribute("x").as_double();
	this->y = pointNode.attribute("y").as_double();
}
Point::~Point() noexcept {

}
// responsibility of the caller to move this point's root somewhere else
Point::Point(const Point& point) noexcept {
	auto parent = point.root.parent();
	this->root = parent.insert_copy_after(point.root, point.root);
	this->setX(point.getX());
	this->setY(point.getY());
}
void Point::setVal(const char* name, double value, double defaultValue) noexcept {
	if(std::abs(value - defaultValue) < EPSILON) this->root.remove_attribute(name);
	else {
		if (this->root.attribute(name).empty()) this->root.append_attribute(name);
		this->root.attribute(name).set_value(value);
	}
}
double Point::getX() const noexcept {
	return this->x;
}
void Point::setX(double x) noexcept {
	this->setVal("x", x);
	this->x = x;
}
double Point::getY() const noexcept {
	return this->y;
}
void Point::setY(double y) noexcept {
	this->setVal("y", y);
	this->y = y;
}
pugi::xml_node& Point::getRoot() noexcept {
	return this->root;
}
const pugi::xml_node& Point::getRoot() const noexcept {
	return this->root;
}
