#include "../include/Point.h"
#include <limits>

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
double Point::getX() const noexcept {
	return this->x;
}
void Point::setX(double x) noexcept {
	if (std::abs(x) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("x");
	else {
		if (this->root.attribute("x").empty()) this->root.append_attribute("x");
		this->root.attribute("x").set_value(x);
	}
}
double Point::getY() const noexcept {
	return this->y;
}
void Point::setY(double y) noexcept {
	if (std::abs(y) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("y");
	else {
		if (this->root.attribute("y").empty()) this->root.append_attribute("y");
		this->root.attribute("y").set_value(y);
	}
}
pugi::xml_node& Point::getRoot() noexcept {
	return this->root;
}
const pugi::xml_node& Point::getRoot() const noexcept {
	return this->root;
}
